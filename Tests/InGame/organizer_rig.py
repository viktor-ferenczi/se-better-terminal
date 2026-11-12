"""In game test of the ship organizer, driven through the Remote plugin's API.

Needs a running client with the Remote and Better Terminal plugins and a blueprint
of a ship with a cockpit, batteries, thrusters and a repair projector (default: Bear).
Pastes the ship, organizes it with the /bt chat command, checks the result, reverts
and checks that the ship is back to what it was. Prints PASS or the failed checks.

    python3 organizer_rig.py --port 25350 --world <save folder> [--blueprint Bear]

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

    cockpit, projector = kind("Cockpit")[0], kind("Projector")[0]
    remote = (kind("RemoteControl") or [None])[0]
    projection_before = projection(api, projector)
    print("projection before:", projection_before, flush=True)

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
    check(not after[cockpit]["terminal"], "the cockpit is hidden from the terminal")
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
        all(after[i]["inventory"] for i in kind("CargoContainer")),
        "cargo containers stay in the inventory screen",
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
    hidden = [i for i, b in after.items() if not b["terminal"] and i != cockpit]
    grouped = {i for ids in groups_after.values() for i in ids}
    check(all(i in grouped for i in hidden), "every hidden block is in a group")

    slots = after[cockpit]["slots"]
    check(
        slots.get(80, (None, None, None))[1:] == (projector, "OnOff"),
        f"slot 9.9 is the repair projector: {slots.get(80)}",
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
        slots.get(11, (None, None, None))[1:] == (batteries, "Recharge"),
        f"slot 2.3 recharges the batteries: {slots.get(11)}",
    )
    check(
        all(
            s[1] in groups_after
            for s in slots.values()
            if s[0].endswith("TerminalGroup")
        ),
        "all toolbar groups exist",
    )

    loaded = projection(api, projector)
    check(
        loaded["organized"], "the repair projector was reloaded with the organized ship"
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
        if reverted.get(i) != block:
            check(False, f"revert of {block['name']}: {reverted.get(i)} != {block}")
            break
    check(
        groups_reverted == groups_before,
        f"revert of groups: {sorted(set(groups_reverted) ^ set(groups_before))}",
    )
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
