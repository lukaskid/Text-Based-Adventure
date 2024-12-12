using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

// Enum for unique IDs for each location
public enum LocationID
{
    HoldingCell,
    HoldingArea,
    PurpleHallway,
    OrangeHallway,
    RedHallway,
    Barracks,
    Armory,
    GreenHallway,
    YellowHallway,
    ControlRoom,
    StorageRoom,
    HangarBay,
    Ship
}

// Represents a location in the game
public class Location
{
    public LocationID ID { get; }
    public string Name { get; }
    public string Description { get; set; }
    public string AlternateDescription { get; set; } // Added for the altered description
    public Dictionary<string, LocationID> Connections { get; }
    public List<string> Items { get; }
    public bool IsLocked { get; set; }

    public Location(LocationID id, string name, string description)
    {
        ID = id;
        Name = name;
        Description = description;
        AlternateDescription = ""; // Default to an empty string (no alternate description)
        Connections = new Dictionary<string, LocationID>();
        Items = new List<string>();
        IsLocked = false;

    }
}


// Main program class
class Program
{
    static Dictionary<LocationID, Location> Locations = new(); // Stores all game locations
    static Location CurrentLocation; // The player's current location
    static HashSet<string> PlayerInventory = new(); // The player's inventory to track collected items
    static bool hasHiddenPurple = false; // Tracks if the player has used the hide command
    static bool bombPlanted = false;
    static bool hasHiddenBomb = false;
    static bool bombTaken = false;
    static bool mustHide = false;
    static bool hangarBayDoorsOpen = false;
    static string controlRoomCode = "1986"; // The correct code for the Control Room 
    

    // Method to print text with delay and word wrapping
    static void PrintWithDelay(string text, int delayMicroseconds = 500, int lineWidth = 80)
    {
        string[] words = text.Split(' ');
        string currentLine = "";

        foreach (string word in words)
        {
            if ((currentLine.Length + word.Length + 1) > lineWidth)
            {
                PrintLineWithHighResDelay(currentLine, delayMicroseconds);
                currentLine = word;
            }
            else
            {
                if (currentLine.Length > 0)
                    currentLine += " ";
                currentLine += word;
            }
        }

        if (currentLine.Length > 0)
        {
            PrintLineWithHighResDelay(currentLine, delayMicroseconds);
        }
    }

    static void PrintLineWithHighResDelay(string line, int delayMicroseconds)
    {
        foreach (char letter in line)
        {
            Console.Write(letter);
            if (delayMicroseconds > 0)
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                while (stopwatch.Elapsed.TotalMilliseconds < delayMicroseconds / 1000.0)
                {
                    // Wait
                }
            }
        }
        Console.WriteLine();
    }

    static void Main()
    {
        LoadLocations("locations.txt"); // Loads locations from the file


        //Intro
        PrintWithDelay("You were flying through the quiet expanse of space, the hum of your ship’s engines a comforting constant against the infinite void. It was supposed to be a routine journey, another charted path among the stars. But out here, the unexpected is always lurking. Without warning, your sensors lit up—a Martian ship, massive and menacing, descending upon you like a predator. Before you could react, a surge of energy engulfed your ship, its systems shutting down in an instant. The last thing you remember is the blinding light of their tractor beam pulling you in.");
        Console.ReadKey(); // Wait for player to press something
        Console.Clear();

        // Start in the initial location (Cell)
        CurrentLocation = Locations[LocationID.HoldingCell];
        DisplayCurrentLocation(); // Display the initial location's details

        // Main game loop
        while (true)
        {
            Console.Write("\n> "); // Command prompt
            string input = Console.ReadLine()?.Trim().ToLower(); // Get and normalize user input

            // Check if input is empty
            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine("Please enter a command.");
                continue;
            }

            // Special mustHide condition
            if (mustHide && input!= "hide")
            {
                PrintWithDelay("Martians, their alien voices sharp and frantic as they flood into the corridor. The smoke begins to clear, and their piercing eyes land on you. There’s no time to react.\r\n\r\nA flurry of movement surrounds you, and the last thing you see is the flash of their weapons before everything fades to black.");
                PrintWithDelay("GAME OVER");
                break; // End the game
            }

            // Parse the input into a command and argument
            string[] parts = input.Split(' ', 2); // Split into "command" and "argument"
            string command = parts[0]; // First part is the command
            string argument = parts.Length > 1 ? parts[1] : null; // Second part is the argument (if exists)

            if ((command == "move" || command == "go" || (command == "move" && argument?.StartsWith("to ") == true)) && argument != null)
            {
                argument = argument.StartsWith("to ") ? argument.Substring(3).Trim() : argument; // Normalize "move to <location>"
                MoveCommand(argument);
            }
            // Take command variations
            else if ((command == "take" || command == "grab" || command == "pick") && argument != null)
            {
                argument = argument.StartsWith("up ") ? argument.Substring(3).Trim() : argument; // Normalize "pick up <item>"
                TakeItem(argument);
            }
            else if ((command == "use" || command == "plant" || command == "place" || command == "put") && argument != null)
            {
                UseItem(argument); // Handle item usage
            }
            else if (command == "hide") // Handle the hide command
            {
                HideCommand(); // Player hides in the current location
            }

            // Exit command to quit the game
            else if (command == "exit") break;



            else if (CurrentLocation.ID == LocationID.ControlRoom && (input == "open hangar bay doors" || input == "open doors" || input == "open bay doors" || input == "use control slider"))
            {
                hangarBayDoorsOpen = true;
                PrintWithDelay("You activate the final control on the panel, and with a deep, reverberating groan, the hangar doors begin to shift. The heavy beams slide apart, revealing a brilliant expanse of the starry void beyond. A powerful rush of air whips through the bay as the atmospheric shield activates, holding the ship's environment intact while allowing the passage of vessels. The faint shimmer of the shield dances against the darkness of space, creating a surreal, almost hypnotic barrier between you and the infinite beyond.");
            }
            else
            {
                PrintWithDelay("Unknown command. Try 'move to <room>', 'hide', 'take <item>', or 'use <item> on <thing>'.");
            }

        }
    }

    // Loads location data from a text file
    static void LoadLocations(string filePath)
    {
        try
        {
            string[] lines = File.ReadAllLines(filePath); // Read all lines from the file
            Location currentLocation = null; // Temporary variable to store the current location being processed

            foreach (string line in lines)
            {

                string trimmedLine = line.Trim(); // Trim whitespace from the line

                if (string.IsNullOrEmpty(trimmedLine)) continue; // Skip empty lines

                // Start of a new location
                if (trimmedLine.StartsWith("Name:"))
                {
                    // Save the previous location before creating a new one
                    if (currentLocation != null && !Locations.ContainsKey(currentLocation.ID))
                    {
                        Locations.Add(currentLocation.ID, currentLocation);
                        // Debug: Console.WriteLine($"Added location: {currentLocation.Name} (ID: {currentLocation.ID})");
                    }

                    // Create a new location
                    string locationName = trimmedLine.Substring(5).Trim(); // Extract the name
                    LocationID id = Enum.Parse<LocationID>(locationName.Replace(" ", "")); // Parse the name into a LocationID
                    currentLocation = new Location(id, locationName, string.Empty); // Initialize the location
                }
                // Set the description of the current location
                else if (trimmedLine.StartsWith("Description:"))
                {
                    currentLocation.Description = trimmedLine.Substring(12).Trim();
                }

                else if (trimmedLine.StartsWith("AlternateDescription:"))
                {
                    currentLocation.AlternateDescription = trimmedLine.Substring(21).Trim();
                }
                // Process normal connections
                else if (trimmedLine.StartsWith("Connections:"))
                {
                    string[] connections = trimmedLine.Substring(12).Trim().Split(','); // Split into connections
                    foreach (string connection in connections)
                    {
                        string connectionName = connection.Trim(); // Trim whitespace
                        try
                        {
                            LocationID connID = Enum.Parse<LocationID>(connectionName.Replace(" ", "")); // Parse the connection
                            currentLocation.Connections[connectionName.ToLower()] = connID; // Add to the dictionary
                            // Debug: Console.WriteLine($"Connection added: {connectionName} -> {connID}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to parse connection '{connectionName}' for location '{currentLocation.Name}': {ex.Message}");
                        }
                    }
                }
                else if (trimmedLine.StartsWith("IsLocked:"))
                {
                    currentLocation.IsLocked = bool.Parse(trimmedLine.Substring(9).Trim());
                }

                // Process items in the location
                else if (trimmedLine.StartsWith("Items:"))
                {
                    string[] items = trimmedLine.Substring(6).Trim().Split(','); // Split into item names
                    foreach (string item in items)
                    {
                        string itemName = item.Trim(); // Trim whitespace
                        if (!string.IsNullOrEmpty(itemName) && itemName != "None")
                        {
                            currentLocation.Items.Add(itemName.ToLower()); // Add to the location's item list
                        }
                    }
                }
                // Debug: Console.WriteLine($"Added location: {currentLocation.Name} (ID: {currentLocation.ID})");

            }

            // Add the last processed location
            // Add the final location after the loop
            if (currentLocation != null && !Locations.ContainsKey(currentLocation.ID))
            {
                Locations.Add(currentLocation.ID, currentLocation);
                // Debug: Console.WriteLine($"Added location: {currentLocation.Name} (ID: {currentLocation.ID})");
            }
            // Debug: Console.WriteLine("Locations loaded successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading locations: {ex.Message}");
        }
    }

    // Displays the current location's details
    static void DisplayCurrentLocation()
    {
        // Check if the current location is the Holding Cell and if the player has the panel
        if (CurrentLocation.ID == LocationID.HoldingCell && PlayerInventory.Contains("panel") && !string.IsNullOrWhiteSpace(CurrentLocation.AlternateDescription))
        {
            // Show the alternate description for Holding Cell
            PrintWithDelay(CurrentLocation.AlternateDescription);
        }
        else if (CurrentLocation.ID == LocationID.HoldingArea && PlayerInventory.Contains("bag") && !string.IsNullOrWhiteSpace(CurrentLocation.AlternateDescription))
        {
            // Show the alternate description for Holding Cell
            PrintWithDelay(CurrentLocation.AlternateDescription);
        }
        // Check if the current location is the Purple Hallway and if the player has hidden there
        else if (CurrentLocation.ID == LocationID.PurpleHallway && hasHiddenPurple == true && !string.IsNullOrWhiteSpace(CurrentLocation.AlternateDescription))
        {
            // Show alternate description for Purple Hallway
            PrintWithDelay(CurrentLocation.AlternateDescription);
        }
        else if (CurrentLocation.ID == LocationID.Armory && bombTaken == true && !string.IsNullOrWhiteSpace(CurrentLocation.AlternateDescription))
        {
            // Show the alternate description for Armory
            PrintWithDelay(CurrentLocation.AlternateDescription);
        }
        else if (CurrentLocation.ID == LocationID.ControlRoom && hangarBayDoorsOpen == true && !string.IsNullOrWhiteSpace(CurrentLocation.AlternateDescription))
        {
            // Show the alternate description for Control Room
            PrintWithDelay(CurrentLocation.AlternateDescription);
        }
        else if (CurrentLocation.ID == LocationID.RedHallway && bombPlanted == true && !string.IsNullOrWhiteSpace(CurrentLocation.AlternateDescription))
        {
            // Show the alternate description for Red Hallway after bombs gone off.
            PrintWithDelay(CurrentLocation.AlternateDescription);
        }
        else if (CurrentLocation.ID == LocationID.HangarBay && bombPlanted == true && hangarBayDoorsOpen == true && !string.IsNullOrWhiteSpace(CurrentLocation.AlternateDescription))
        {
            // Show the alternate description for Hangar Bay
            PrintWithDelay(CurrentLocation.AlternateDescription);
        }
        else if (CurrentLocation.ID == LocationID.HangarBay && hangarBayDoorsOpen == true && bombPlanted == false && !string.IsNullOrWhiteSpace(CurrentLocation.AlternateDescription))
        {
            // Show the alternate description for Hangar Bay but bomb hasn't been planted 
            PrintWithDelay(CurrentLocation.AlternateDescription + " But the Martian's below remain.");
        }
        else if (CurrentLocation.ID == LocationID.Ship && bombPlanted == true && hangarBayDoorsOpen == false && !string.IsNullOrWhiteSpace(CurrentLocation.AlternateDescription))
        {
            // Show the alternate description for Hangar Bay but the hangar doors havent been oppened
            PrintWithDelay(CurrentLocation.AlternateDescription + " But the the hangar bay doors remain closed.");
        }
        else if (CurrentLocation.ID == LocationID.Ship && !PlayerInventory.Contains("bag") && hangarBayDoorsOpen == false && !string.IsNullOrWhiteSpace(CurrentLocation.AlternateDescription))
        {
            // Show the alternate description for Hangar Bay but the hangar doors havent been oppened
            PrintWithDelay(CurrentLocation.AlternateDescription);
            PrintWithDelay("GAME OVER");
            Environment.Exit(0); // Ends the game
        }
        else if (CurrentLocation.ID == LocationID.Ship && PlayerInventory.Contains("bag") && hangarBayDoorsOpen == true && !string.IsNullOrWhiteSpace(CurrentLocation.AlternateDescription))
        {
            // Show the alternate description for Hangar Bay but the hangar doors havent been oppened
            PrintWithDelay(CurrentLocation.Description);
            PrintWithDelay("The engines roar to full power as your ship surges forward, breaking free from the Martian vessel. The sight of the alien hangar disappears from view, replaced by the open void of space. You’ve done it. Against all odds, you’ve outsmarted them, escaped their clutches, and taken back your freedom. Your hands steady on the controls as the ship’s systems hum back to life, a symphony of victory. The Martians are behind you now, and with the stars ahead, you know you’ve won.\r\n\r\nYou’ve escaped. Victory is yours.");
            Environment.Exit(0); // Ends the game
        }
        else
        {
            // Show the default description for other locations
            PrintWithDelay(CurrentLocation.Description);
        }
        /* Debug
                // List available connections
                Console.WriteLine("\nConnections:");
                foreach (var connection in CurrentLocation.Connections)
                {
                    Console.WriteLine($"- {connection.Key}");
                }

                // List items in the location
                if (CurrentLocation.Items.Count > 0)
                {
                    Console.WriteLine("\nItems in this location:");
                    foreach (var item in CurrentLocation.Items)
                    {
                        Console.WriteLine($"- {item}");
                    }
                }


        // List player's inventory
        if (PlayerInventory.Count > 0)
        {
            Console.WriteLine("\nYour Inventory:");
            foreach (var item in PlayerInventory)
            {
                Console.WriteLine($"- {item}");
            }
        }
                */
    }


    // Handles moving between locations<
    static void MoveCommand(string targetLocation)
    {
        targetLocation = targetLocation.ToLower().Replace(" ", "");

        // Special condition for entering Green Hallway from Purple Hallway
        if (CurrentLocation.ID == LocationID.PurpleHallway && targetLocation == "greenhallway")
        {
            if (!hasHiddenPurple)
            {
                PrintWithDelay("You step into the hallway, only to collide with something solid. Before you can react, you look up to see the alien towering over you, its glowing eyes locking onto yours. With a swift, brutal motion, it raises a weapon, and in an instant, a searing pain rips through your body. The world goes black as your life fades away.");
                PrintWithDelay("GAME OVER");
                Environment.Exit(0); // Ends the game
            }
        }

        // Special condition for entering Ship before planting the bomb
        if (CurrentLocation.ID == LocationID.HangarBay && !bombPlanted && targetLocation == "ship")
        {
            PrintWithDelay("You step out onto the hangar floor, your heart pounding as you make a break for your ship. The sound of your hurried footsteps echoes in the vast space, immediately drawing the attention of the Martians. They turn, their glowing eyes locking onto you in unison. A sharp, guttural cry pierces the air as they drop their tools and move with alarming speed. Before you can react, a searing energy blast strikes you, sending pain ripping through your body. Your vision fades as you collapse to the cold metal floor, the last thing you see being the Martians closing in, their forms blurring into darkness.");
            PrintWithDelay("GAME OVER");
            Environment.Exit(0); // End the game
        }


        foreach (var connection in CurrentLocation.Connections)
        {
            string normalizedConnectionName = connection.Key.ToLower().Replace(" ", "");
            if (normalizedConnectionName == targetLocation)
            {
                LocationID nextLocationID = connection.Value;
                Location nextLocation = Locations[nextLocationID];

                // Special condition for entering the Barracks
                if (nextLocation.ID == LocationID.Barracks)
                {
                    PrintWithDelay("The barrack doors slide open with a low hiss, and you step inside, your senses immediately overwhelmed by the humid, stale air. Before you can take another step, movement catches your eye—dozens of aliens turn to face you, their glowing eyes narrowing as one. A guttural, unified hiss fills the room, and within seconds, they lunge. You barely have time to react as they swarm, their speed and precision overwhelming. Pain erupts as they strike, and the world fades into darkness. Your last thought is a bitter realization—you walked straight into their den.");
                    PrintWithDelay("GAME OVER");
                    Console.ReadKey();
                    Environment.Exit(0); // End the game immediately
                }



                // Special condition for entering the Control Room
                if (nextLocation.ID == LocationID.ControlRoom && nextLocation.IsLocked)
                {
                    PrintWithDelay("The Control Room is locked. You'll have to enter a code to unlock it:");

                    Console.Write("Enter code: ");
                    string codeInput = Console.ReadLine()?.Trim();

                    if (codeInput == controlRoomCode)
                    {
                        PrintWithDelay("With a faint beep, the glyphs on the control room door flash and fade. A mechanical hiss follows as the door slides open, revealing the dimly lit interior and the hum of alien machinery within.");
                        nextLocation.IsLocked = false; // Unlock the Control Room
                    }
                    else
                    {
                        Console.WriteLine("No, that's not it.");
                        return; // Exit without moving to the Control Room
                    }
                }
                // Move to the new location (if unlocked or not locked at all)
                if (nextLocation.IsLocked)
                {
                    PrintWithDelay($"You can't seem to get past.");
                    return;
                }
                if (!nextLocation.IsLocked)
                {
                    CurrentLocation = nextLocation;
                    // Debug: PrintWithDelay($"\nYou move to {connection.Key}.\n");
                    DisplayCurrentLocation();
                    return;
                }
            }
        }
    PrintWithDelay($"You can't move to '{targetLocation}'.");
    }

    // Handles item collection
    static void TakeItem(string itemName)
    {
        itemName = itemName.ToLower(); // Normalize the item name

        // Check if the item is in the current location
        if (CurrentLocation.Items.Remove(itemName))
        {
            PlayerInventory.Add(itemName); // Add the item to the inventory

            // Panel
            if (itemName == "panel")
            {
                PrintWithDelay("With some effort, you pry up the misaligned floor panel.The metal is surprisingly light but sturdy, its edges sharp enough to make you grip it carefully.");
            }
            //
            if (itemName == "bag")
            {
                PrintWithDelay("You approach the locker containing your bag, its translucent panel glowing faintly. Up close, the locking mechanism becomes clearer—an array of alien glyphs, pulsing in an irregular rhythm. Tentatively, you press your hand to the surface. For a moment, nothing happens, and then the glyphs flare brightly and dissolve into a shimmering mist. The panel slides open with a soft hiss. The bag is heavier than you remember, its weight grounding you in the moment. As you inspect its contents, relief washes over you—most of your belongings are intact - and most importantly, the keys to you ship. There’s also a faint residue of static energy clinging to the fabric, likely from its time aboard this alien vessel. As you sling the bag over your shoulder, the ambient hum of the ship seems to shift, growing slightly louder and more insistent. The tension in the air thickens.");
            }
            if (itemName == "bomb")
            {
                PrintWithDelay("Picking it up, you feel the weight of its potential in your hands — a device capable of immense devastation. A small, blinking indicator on its side reassures you that it’s functional. You can almost imagine the chaos it could unleash if placed strategically — like the door of a place where they'd congrigate the most. Such an explosion could not only take them out of commision but might also draw attention away, giving you a chance to slip unnoticed toward your ship.");
                bombTaken = true;
            }
        }
        else
        {
            PrintWithDelay($"There is no '{itemName}' here.");
        }



    }

    // Handles item usage
    static void UseItem(string input)
    {
        string[] parts = input.Split(" on ");
        if (parts.Length != 2)
        {
            PrintWithDelay("I don't think that will do anything.");
            return;
        }

        string itemName = parts[0].Trim().ToLower();
        string target = parts[1].Trim().ToLower();

        if (!PlayerInventory.Contains(itemName))
        {
            PrintWithDelay($"You don't have a '{itemName}' to use.");
            return;
        }

        // Cell door
        if (CurrentLocation.ID == LocationID.HoldingCell && itemName == "panel" && (target == "energy emitter" || target == "emitter" || target == "door"))
        {
            PrintWithDelay("Holding it tightly, you approach one of the energy emitters embedded in the wall. The panel vibrates faintly in your hands as you brace yourself and swing with all your might.The impact lands with a sharp clang, followed by a loud, sputtering crackle. Sparks burst from the emitter, casting erratic shadows across the cell. The energy barrier flickers violently, its once-steady hum now erratic and dissonant.Encouraged, you strike again. This time, the barrier’s glow fades to a dim, wavering shimmer, weak enough to suggest it might no longer be impassable. The emitter sparks one final time, then falls silent, its glow extinguished. With the barrier gone you can now move into the holding area");

            if (Locations[LocationID.HoldingArea].IsLocked)
            {
                Locations[LocationID.HoldingArea].IsLocked = false;
            }

            return;
        }
        if (CurrentLocation.ID == LocationID.RedHallway && itemName == "bomb" && (target == "barracks" || target == "barracks boor"))
        {
            PrintWithDelay("You crouch by the barrack doors, the smooth metal surface cold under your fingertips. Reaching into your bag, you pull out the bomb. You attach it to the center of the door, the adhesive clinging tightly as the device hums to life. A faint series of beeps starts as the bomb’s timer activates, the rhythm quickening with each passing second. Stepping back, you clutch the detonator, your heart pounding in sync with the bomb’s countdown. The room seems to hold its breath, the only sound the escalating tempo of the beeping. Then, with a deep breath, you press the button. A deafening explosion rocks the hallway, and the air fills with the sharp scent of scorched metal and a wave of searing heat. The barrack doors are blown inward, molten fragments scattering across the floor. The whole ship has been alerted.");
            PlayerInventory.Remove("bomb");
            bombPlanted = true;
            // Set the flag requiring the player to hide
            mustHide = true;
            return;
        }
        PrintWithDelay("That doesn't seem to do anything.");
    }

    static void HideCommand()
    {
        // Check if the player is in the Purple Hallway
        if (CurrentLocation.ID == LocationID.PurpleHallway)
        {
            hasHiddenPurple = true;
            PrintWithDelay("As the faint sound of footsteps echoes from the green hallway, your pulse quickens. The shadows in the purple corridor seem to ripple with the ship’s dim, iridescent glow, offering just enough cover to conceal yourself. Quickly, you press against the smooth metallic wall, slipping into a shallow recess where the lighting flickers unevenly. You hold your breath, the hum of the ship’s energy thrumming in your ears as the steps grow louder, more deliberate. The alien comes into view—a tall, wiry figure with glowing eyes scanning the corridor. Its movements are precise, almost mechanical, as it strides past you, oblivious to your presence. For a moment, it pauses, its head tilting as if sensing something. Your heart pounds, but you remain perfectly still. Then, just as suddenly as it appeared, the alien continues down the hallway, the sound of its steps fading into the distance. You exhale shakily, your hands trembling slightly as you step out from your hiding spot. The danger has passed — for now.");
        }
        //hides after bomb explosion
        else if (mustHide == true)
        {
            PrintWithDelay("The Martians rush past, their wiry forms darting toward the barracks, weapons at the ready. They hiss and chatter in their alien tongue, their urgency overriding any suspicion of your presence. One by one, they disappear through the shattered remains of the barrack doors, their focus entirely on the destruction inside. The hallway falls silent once more, save for the faint crackling of damaged conduits and the distant hum of the ship’s systems. Your heart pounds, but the path ahead is clear — for now.");
            mustHide = false; // Reset the mustHide flag
        }
        else
        {
            // General response when hiding isn't necessary
            PrintWithDelay("You find a quiet spot to hide, but there's no immediate danger.");
        }

    }
}