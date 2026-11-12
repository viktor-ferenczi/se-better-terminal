using System;
using System.Collections.Generic;

namespace ClientPlugin.Organizer
{
    // Block classification and English naming rules, free of game types
    public static class Naming
    {
        // Default block names longer than this get shortened to leave room for suffixes
        public const int MaxNameLength = 24;

        // Words dropped from long names: they tell the look of the block, not what it does
        private static readonly string[] StyleWords =
        {
            "Sci-Fi",
            "Industrial",
            "Warfare",
            "Colorable",
            "Automaton",
            "Offroad",
            "Reinforced",
            "Suspension",
            "Shape",
        };

        // Abbreviations for long names, tried in this order after dropping the style words
        private static readonly string[][] Abbreviations =
        {
            new[] { "Atmospheric", "Atmo" },
            new[] { "Programmable", "Prog." },
            new[] { "Hydrogen", "H2" },
            new[] { "Oxygen", "O2" },
            new[] { "Cargo Container", "Container" },
            new[] { "Large", "L." },
            new[] { "Small", "S." },
        };

        public static string Shorten(string name)
        {
            name = (name ?? "").Trim();
            if (name.Length <= MaxNameLength)
                return name;

            // The last word names the main type of the block ("Thruster", "Light"), it always stays
            var words = new List<string>(
                name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
            );
            for (var i = words.Count - 2; i >= 0 && Length(words) > MaxNameLength; i--)
            {
                if (Array.IndexOf(StyleWords, words[i]) >= 0)
                    words.RemoveAt(i);
            }

            name = string.Join(" ", words);
            foreach (var abbreviation in Abbreviations)
            {
                if (name.Length <= MaxNameLength)
                    break;

                name = name.Replace(abbreviation[0], abbreviation[1]);
            }

            return name;
        }

        private static int Length(List<string> words)
        {
            var length = words.Count - 1;
            foreach (var word in words)
                length += word.Length;
            return length;
        }

        public static string Plural(string name)
        {
            if (name.EndsWith("y") && name.Length > 1 && "aeiou".IndexOf(name[name.Length - 2]) < 0)
                return name.Substring(0, name.Length - 1) + "ies";

            if (
                name.EndsWith("s")
                || name.EndsWith("x")
                || name.EndsWith("ch")
                || name.EndsWith("sh")
            )
                return name + "es";

            return name + "s";
        }

        // Dominant direction of a vector given in the cockpit's frame: X right, Y up, Z back
        public static ShipDirection DirectionOf(double x, double y, double z)
        {
            var ax = Math.Abs(x);
            var ay = Math.Abs(y);
            var az = Math.Abs(z);
            if (ax < 0.5 && ay < 0.5 && az < 0.5)
                return ShipDirection.None;

            if (az >= ax && az >= ay)
                return z < 0 ? ShipDirection.Front : ShipDirection.Back;

            if (ay >= ax)
                return y > 0 ? ShipDirection.Top : ShipDirection.Bottom;

            return x > 0 ? ShipDirection.Right : ShipDirection.Left;
        }

        // The game names thrusters by the direction they accelerate the ship to
        public static string ThrustDirectionName(ShipDirection direction)
        {
            switch (direction)
            {
                case ShipDirection.Front:
                    return "Forward";
                case ShipDirection.Back:
                    return "Backward";
                case ShipDirection.Top:
                    return "Up";
                case ShipDirection.Bottom:
                    return "Down";
                default:
                    return direction.ToString();
            }
        }

        public static string ThrusterLabel(string thrusterType)
        {
            switch (thrusterType)
            {
                case null:
                case "":
                    return "Thruster";
                case "Atmospheric":
                    return "Atmo Thruster";
                default:
                    return thrusterType + " Thruster";
            }
        }

        // typeId is without the MyObjectBuilder_ prefix
        public static BlockKind Classify(string typeId, string subtypeId, bool canControlShip)
        {
            switch (typeId)
            {
                case "Thrust":
                    return BlockKind.Thruster;
                case "Gyro":
                    return BlockKind.Gyro;
                case "BatteryBlock":
                    return BlockKind.Battery;
                case "InteriorLight":
                    return BlockKind.Light;
                case "ReflectorLight":
                    return BlockKind.Spotlight;
                case "Drill":
                    return BlockKind.Drill;
                case "ShipWelder":
                    return BlockKind.Welder;
                case "ShipGrinder":
                    return BlockKind.Grinder;
                case "MotorSuspension":
                    return BlockKind.Wheel;
                case "CameraBlock":
                    return BlockKind.Camera;
                case "ShipConnector":
                    // Ejectors are connectors by type, but cannot dock
                    return subtypeId == "ConnectorSmall" || (subtypeId ?? "").Contains("Ejector")
                        ? BlockKind.Ejector
                        : BlockKind.Connector;
                case "LandingGear":
                    return BlockKind.LandingGear;
                case "Cockpit":
                case "CryoChamber":
                    return canControlShip ? BlockKind.Cockpit : BlockKind.Seat;
                case "RemoteControl":
                    return BlockKind.RemoteControl;
                case "Projector":
                    return BlockKind.Projector;
                case "JumpDrive":
                    return BlockKind.JumpDrive;
                case "OxygenTank":
                    return BlockKind.GasTank;
                case "OxygenGenerator":
                    return BlockKind.GasGenerator;
                case "HydrogenEngine":
                    return BlockKind.HydrogenEngine;
                case "Reactor":
                    return BlockKind.Reactor;
                case "SolarPanel":
                    return BlockKind.SolarPanel;
                case "WindTurbine":
                    return BlockKind.WindTurbine;
                case "CargoContainer":
                    return BlockKind.Cargo;
                case "Parachute":
                    return BlockKind.Parachute;
                case "LargeGatlingTurret":
                case "LargeMissileTurret":
                case "InteriorTurret":
                    return BlockKind.Turret;
                case "SmallGatlingGun":
                case "SmallMissileLauncher":
                case "SmallMissileLauncherReload":
                    return BlockKind.Gun;
                case "Decoy":
                    return BlockKind.Decoy;
                case "Door":
                case "AirtightHangarDoor":
                case "AirtightSlideDoor":
                    return BlockKind.Door;
                case "AirVent":
                    return BlockKind.AirVent;
                case "ConveyorSorter":
                    return BlockKind.Sorter;
                case "Collector":
                    return BlockKind.Collector;
                case "ButtonPanel":
                    return BlockKind.ButtonPanel;
                default:
                    return BlockKind.Other;
            }
        }
    }
}
