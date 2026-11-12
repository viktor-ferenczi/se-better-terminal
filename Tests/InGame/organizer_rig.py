"""In game test of the ship organizer, driven through the Remote plugin's API.

Needs a running client with the Remote and Better Terminal plugins and a blueprint
of a ship with a cockpit or a remote control, batteries and thrusters (default: Bear).
Pastes the ship, organizes it with the /bt chat command, checks the result, reverts
and checks that the ship is back to what it was. Prints PASS or the failed checks.

    python3 organizer_rig.py --port <Remote port> --world <save folder> [--blueprint Bear]

se_remote.py of the se-remote skill must be importable (PYTHONPATH).
"""

import argparse
import re
import sys
import time
import xml.etree.ElementTree as ET

from se_remote import RemoteAPI

XSI_TYPE = "{http://www.w3.org/2001/XMLSchema-instance}type"

failures = []


def check(condition, what):
    if not condition:
        failures.append(what)
        print("FAILED:", what, flush=True)


def snapshot(api, grid_id):
    """Names, visibility, toolbars and groups of the ship, by block entity id."""
    ids, offset = [], 0
    while True:
        page = api.list_cubes(grid_id, offset, 200)
        ids += [cube["fatEntityId"] for cube in page["cubes"] if cube["fatEntityId"]]
        offset += 200
        if offset >= page["total"]:
            break

    state = {}
    for entity_id in ids:
        xml = api.get_object_builder(entity_id)
        root = ET.fromstring(xml)
        if root.find("ShowInTerminal") is None and root.find("CustomName") is None:
            continue
        slots = {}
        for slot in root.findall("Toolbar/Slots/Slot"):
            data = slot.find("Data")
            slots[int(slot.findtext("Index"))] = (
                data.get(XSI_TYPE),
                data.findtext("GroupName") or int(data.findtext("BlockEntityId") or 0),
                data.findtext("Action"),
            )
        state[entity_id] = {
            "type": root.get(XSI_TYPE) or root.tag,
            "subtype": root.findtext("SubtypeName") or "",
            "name": root.findtext("CustomName"),
            "terminal": root.findtext("ShowInTerminal") != "false",
            "inventory": root.findtext("ShowInInventory") != "false",
            "slots": slots,
        }
    groups = {name: sorted(ids) for name, ids in api.list_block_groups(grid_id).items()}
    return state, groups


def projection(api, projector_id):
    """What the projector holds: is it the organized ship, its alignment and its switch."""
    xml = api.get_object_builder(projector_id)
    own, _, projected = xml.partition("<ProjectedGrid")
    settings = xml.rpartition("</ProjectedGrid")[2]

    def text(tag):
        match = re.search(
            rf"<{tag}>(.*?)</{tag}>",
            settings if tag.startswith("Projection") else own,
            re.S,
        )
        return re.sub(r"\s+", "", match.group(1)) if match else None

    return {
        "blocks": len(re.findall(r"<MyObjectBuilder_CubeBlock", projected)),
        "organized": "Projector Repair" in projected,
        "offset": text("ProjectionOffset"),
        "rotation": text("ProjectionRotation"),
        "enabled": text("Enabled"),
    }


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--port", type=int, default=24158)
    parser.add_argument("--password", default="SpaceEngineers")
    parser.add_argument("--world", required=True)
    parser.add_argument("--blueprint", default="Bear")
    args = parser.parse_args()

    api = RemoteAPI(
        f"http://127.0.0.1:{args.port}", username="admin", password=args.password
    )
    api.wait_for_api(max_wait=300)
    api.load(args.world)
    api.wait_for_state(ready=True, max_wait=300)

    grids = api.paste_blueprint(name=args.blueprint)
    grid_id = grids[0]["entityId"]
    ship = grids[0]["name"]
    time.sleep(5)  # The projector starts projecting a few frames after the paste

    before, groups_before = snapshot(api, grid_id)
    print(
        f"pasted {ship}: {len(before)} terminal blocks, {len(groups_before)} groups",
        flush=True,
    )

    def kind(type_name):
        return [
            i for i, b in before.items() if b["type"] == "MyObjectBuilder_" + type_name
        ]

    # A drone has no cockpit: its remote control gets the toolbar
    cockpit = (kind("Cockpit") + kind("RemoteControl"))[0]
    remote = (
        kind("RemoteControl")[0] if kind("Cockpit") and kind("RemoteControl") else None
    )
    projector = (kind("Projector") or [None])[0]
    projection_before = projection(api, projector) if projector else None
    print("projection before:", projection_before, flush=True)
    if projector and not projection_before["blocks"]:
        projector = None  # Holds no blueprint, so it is not a repair projector

    # Organize
    print(api.send_chat(f"/bt organize {grid_id}"), flush=True)
    time.sleep(8)  # The projector reload takes a few frames
    after, groups_after = snapshot(api, grid_id)
    names = [b["name"] for b in after.values()]
    prefix = ship + " - "

    check(len(set(names)) == len(names), "names are unique")
    check(
        all(n.startswith(prefix) for n in names), "all names start with the ship name"
    )
    if projector:
        check(
            after[projector]["name"] == prefix + "Projector Repair",
            f"repair projector name: {after[projector]['name']}",
        )
    for type_name in (
        "Thrust",
        "Gyro",
        "BatteryBlock",
        "CameraBlock",
        "ReflectorLight",
    ):
        check(
            all(not after[i]["terminal"] for i in kind(type_name)),
            f"{type_name} blocks are hidden from the terminal",
        )
    if kind("Cockpit"):
        check(not after[cockpit]["terminal"], "the cockpit is hidden from the terminal")
    # Hidden without a group
    refills = [
        i for i in kind("MedicalRoom") if "RefillStation" in before[i]["subtype"]
    ]
    ungrouped = set(kind("Cockpit") + kind("TextPanel") + kind("ButtonPanel") + refills)
    if kind("TextPanel"):
        check(
            all(not after[i]["terminal"] for i in kind("TextPanel")),
            "LCDs are hidden from the terminal",
        )
    if refills:
        check(
            all(not after[i]["terminal"] for i in refills),
            "refill stations are hidden from the terminal",
        )
    if remote:
        check(after[remote]["terminal"], "the remote control stays in the terminal")
    cameras = sorted(after[i]["name"] for i in kind("CameraBlock"))
    check(
        all(
            re.search(r" Camera (Front|Back|Left|Right|Top|Bottom)( \d+)?$", n)
            for n in cameras
        ),
        f"camera names: {cameras}",
    )
    connectors = sorted(after[i]["name"] for i in kind("ShipConnector"))
    check(
        all(
            re.search(r" Connector (Front|Back|Left|Right|Top|Bottom)( \d+)?$", n)
            for n in connectors
        ),
        f"connector names: {connectors}",
    )
    check(
        all(not after[i]["inventory"] for i in kind("ShipConnector")),
        "connectors are hidden from the inventory screen",
    )
    check(
        not kind("CargoContainer")
        or any(after[i]["inventory"] for i in kind("CargoContainer")),
        "the main cargo container stays in the inventory screen",
    )

    batteries = prefix + "Batteries"
    check(
        sorted(groups_after.get(batteries, [])) == sorted(kind("BatteryBlock")),
        "the Batteries group has all batteries",
    )
    thruster_groups = [
        g
        for g in groups_after
        if re.search(r"Thrusters \((Up|Down|Left|Right|Forward|Backward)\)$", g)
    ]
    check(len(thruster_groups) >= 1, f"thruster groups by direction: {thruster_groups}")
    hidden = [i for i, b in after.items() if not b["terminal"] and i not in ungrouped]
    grouped = {i for ids in groups_after.values() for i in ids}
    check(all(i in grouped for i in hidden), "every hidden block is in a group")

    slots = after[cockpit]["slots"]
    tools = kind("Drill") + kind("ShipWelder") + kind("ShipGrinder")
    guns = (
        kind("SmallGatlingGun")
        + kind("SmallMissileLauncher")
        + kind("SmallMissileLauncherReload")
    )
    # A connector's camera faces the same side, on page 1 and next to its lock on page 2
    for connector in kind("ShipConnector"):
        side = after[connector]["name"].split(" Connector ")[1].split()[0]
        camera = next(
            (i for i in kind("CameraBlock") if f" Camera {side}" in after[i]["name"]),
            None,
        )
        index = next(
            (
                i
                for i, s in slots.items()
                if 9 <= i < 18 and s[1:] == (connector, "SwitchLock")
            ),
            None,
        )
        check(index is not None, f"page 2 locks {after[connector]['name']}")
        if camera and index:
            check(
                slots.get(index - 1, (None, None, None))[1:] == (camera, "View"),
                f"page 2 has the {side} camera before the connector",
            )
            if not (guns and not tools):
                check(
                    any(s[1:] == (camera, "View") for i, s in slots.items() if i < 6),
                    f"page 1 has the {side} docking camera",
                )
    if projector:
        check(
            slots.get(80, (None, None, None))[1:] == (projector, "OnOff"),
            f"slot 9.9 is the repair projector: {slots.get(80)}",
        )

    # The role of the ship: the tool or the main weapon is on 1.1, then the reverse thrusters
    page1 = [slots[i] for i in sorted(slots) if i < 9]
    print("page 1:", page1, flush=True)
    if tools or guns:
        check(
            page1[0][0].endswith("ToolbarItemWeapon"),
            f"slot 1.1 is the tool or the main weapon: {page1[0]}",
        )
    reverse = [g for g in groups_after if g.endswith("Thrusters (Backward)")]
    if reverse:
        check(
            any(s[1] in reverse and s[2] == "OnOff" for s in page1),
            "page 1 switches the reverse thrusters",
        )
    if guns and not tools:
        check(
            not any(s[2] == "SwitchLock" for s in page1),
            "a fighter has no docking on page 1",
        )
        front = [i for i in kind("CameraBlock") if " Camera Front" in after[i]["name"]]
        check(
            not front
            or any(s[1:] == (front[0], "View") for s in page1)
            or len(front) > 1,
            "a fighter has a forward camera on page 1",
        )
    if remote:
        check(
            slots.get(79, (None, None, None))[1:] == (remote, "Control"),
            f"slot 9.8 takes control of the remote control: {slots.get(79)}",
        )
        expected_remote = {i: s for i, s in slots.items() if i != 79}
        check(
            after[remote]["slots"] == expected_remote,
            "the remote control has the cockpit's toolbar without the Control button",
        )
    check(
        slots.get(18, (None, None, None))[1:] == (batteries, "Recharge"),
        f"slot 3.1 recharges the batteries: {slots.get(18)}",
    )
    check(
        all(
            s[1] in groups_after
            for s in slots.values()
            if s[0].endswith("TerminalGroup")
        ),
        "all toolbar groups exist",
    )

    if projector:
        loaded = projection(api, projector)
        check(
            loaded["organized"],
            "the repair projector was reloaded with the organized ship",
        )
        check(
            loaded["enabled"] == projection_before["enabled"],
            "the repair projector's switch is as it was",
        )
        print("projection organized:", loaded, flush=True)

        # Turned on, the projection covers the ship: every projected block is built
        position = tuple(
            next(
                c["min"]
                for c in api.list_cubes(grid_id, 0, 1000)["cubes"]
                if c["fatEntityId"] == projector
            )
        )
        api.set_enabled(grid_id, position, True)
        time.sleep(8)
        info = api.get_block(grid_id, position).get("detailedInfo") or ""
        progress = re.search(r"Build progress: (\d+)/(\d+)", info)
        check(
            progress is not None and progress.group(1) == progress.group(2) != "0",
            f"the projection covers the ship: {info!r}",
        )
        api.set_enabled(grid_id, position, projection_before["enabled"] == "true")
        time.sleep(3)

    # Organizing again changes nothing
    api.send_chat(f"/bt organize {grid_id}")
    time.sleep(8)
    again, groups_again = snapshot(api, grid_id)
    check(
        again == after and groups_again == groups_after,
        "organizing twice gives the same result",
    )

    # Each run made a backup. The first revert returns to the state before the second run.
    api.send_chat(f"/bt revert {grid_id}")
    time.sleep(8)
    once, groups_once = snapshot(api, grid_id)
    check(
        once == after and groups_once == groups_after,
        "the first of two reverts keeps the organized state",
    )

    # Revert
    print(api.send_chat(f"/bt revert {grid_id}"), flush=True)
    time.sleep(8)
    reverted, groups_reverted = snapshot(api, grid_id)
    for i, block in before.items():
        if block["name"] is None:
            # Never renamed: the game saves the default name once it is set again
            block = dict(block, name=reverted[i]["name"])
        if reverted.get(i) != block:
            check(False, f"revert of {block['name']}: {reverted.get(i)} != {block}")
            break
    check(
        groups_reverted == groups_before,
        f"revert of groups: {sorted(set(groups_reverted) ^ set(groups_before))}",
    )
    if projector:
        projection_reverted = projection(api, projector)
        check(
            projection_reverted == projection_before,
            f"revert of the repair projector: {projection_reverted}",
        )

    print(
        "PASS" if not failures else f"FAIL: {len(failures)} checks failed", flush=True
    )
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
