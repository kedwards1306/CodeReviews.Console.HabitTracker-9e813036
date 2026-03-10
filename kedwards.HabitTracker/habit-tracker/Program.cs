using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Data.Sqlite;

namespace habit_tracker
{
    public class Program
    {
        static string connectionString = @"Data Source=habit-Tracker.db";

        static void Main(string[] args)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                var tableCmd = connection.CreateCommand();

                tableCmd.CommandText =
                    @"CREATE TABLE IF NOT EXISTS habits(
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        Unit TEXT NOT NULL
                    )";
                tableCmd.ExecuteNonQuery();

                tableCmd.CommandText =
                    @"CREATE TABLE IF NOT EXISTS habit_logs(
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        HabitId INTEGER NOT NULL,
                        Date TEXT NOT NULL,
                        Quantity INTEGER NOT NULL,
                        FOREIGN KEY (HabitId) REFERENCES habits(Id)
                    )";
                tableCmd.ExecuteNonQuery();

                connection.Close();
            }

            SeedData();
            GetUserInput();
        }

        //SEEDING 

        static void SeedData()
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                var checkCmd = connection.CreateCommand();
                checkCmd.CommandText = "SELECT COUNT(*) FROM habits";
                int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                if (count > 0) return;

                var habits = new List<(string Name, string Unit)>
                {
                    ("Drinking Water", "Glasses"),
                    ("Coding",        "Hours"),
                    ("Reading",        "Pages"),
                    ("Sleep",          "Hours"),
                    ("Meditation",     "Minutes")
                };

                foreach (var habit in habits)
                {
                    var cmd = connection.CreateCommand();
                    cmd.CommandText = "INSERT INTO habits(Name, Unit) VALUES(@name, @unit)";
                    cmd.Parameters.AddWithValue("@name", habit.Name);
                    cmd.Parameters.AddWithValue("@unit", habit.Unit);
                    cmd.ExecuteNonQuery();
                }

                var habitIds = new List<int>();
                var getIdsCmd = connection.CreateCommand();
                getIdsCmd.CommandText = "SELECT Id FROM habits";
                var reader = getIdsCmd.ExecuteReader();
                while (reader.Read())
                {
                     habitIds.Add(reader.GetInt32(0));
                }
                reader.Close();

                var random = new Random();
                for (int i = 0; i < 100; i++)
                {
                    int randomHabitId = habitIds[random.Next(habitIds.Count)];
                    int daysAgo = random.Next(0, 365);
                    string date = DateTime.Today.AddDays(-daysAgo).ToString("dd-MM-yy");
                    int quantity = random.Next(1, 21);

                    var insertLogCmd = connection.CreateCommand();
                    insertLogCmd.CommandText =
                        "INSERT INTO habit_logs(HabitId, Date, Quantity) VALUES(@habitId, @date, @quantity)";
                    insertLogCmd.Parameters.AddWithValue("@habitId", randomHabitId);
                    insertLogCmd.Parameters.AddWithValue("@date", date);
                    insertLogCmd.Parameters.AddWithValue("@quantity", quantity);
                    insertLogCmd.ExecuteNonQuery();
                }

                Console.WriteLine("Database seeded with 5 habits and 100 log entries! Press any key to continue.");
                Console.ReadKey();
            }
        }

        // MENUS

        static void GetUserInput()
        {
            bool closeApp = false;
            while (!closeApp)
            {
                Console.Clear();
                Console.WriteLine("\n\nMAIN MENU");
                Console.WriteLine("\nWhat would you like to do?");
                Console.WriteLine("\nType 0 to close application.");
                Console.WriteLine("Type 1 to Manage Habits.");
                Console.WriteLine("Type 2 to Manage Logs.");
                Console.WriteLine("--------------------------------------\n");

                switch (Console.ReadLine())
                {
                    case "0":
                        Console.WriteLine("\nGoodbye");
                        closeApp = true;
                        break;
                    case "1":
                        HabitMenu();
                        break;
                    case "2":
                        LogMenuPickHabit();
                        break;
                    default:
                        Console.WriteLine("\nInvalid Command. Please type a number from 0 to 2.\n");
                        Console.ReadKey();
                        break;
                }
            }
        }

        static void HabitMenu()
        {
            bool back = false;
            while (!back)
            {
                Console.WriteLine("MANAGE HABITS");
                Console.WriteLine("0 - Back");
                Console.WriteLine("1 - View All Habits");
                Console.WriteLine("2 - Create Habit");
                Console.WriteLine("3 - Update Habit");
                Console.WriteLine("4 - Delete Habit");
                Console.WriteLine("-----------------------");

                switch (Console.ReadLine())
                {
                    case "0": back = true; break;
                    case "1": GetAllRecords(); break;
                    case "2": CreateHabit(); break;
                    case "3": UpdateHabit(); break;
                    case "4": DeleteHabit(); break;
                    default:
                        Console.WriteLine("Invalid option. Press any key to try again.");
                        Console.ReadKey();
                        break;
                }
            }
        }

        static void LogMenuPickHabit()
        {
            var habits = GetAllHabits();

            if (habits.Count == 0)
            {
                Console.WriteLine("\nNo habits found. Please create a habit first. Press any key to go back.");
                Console.ReadKey();
                return;
            }

            Console.Clear();
            Console.WriteLine("SELECT A HABIT");
            foreach (var h in habits)
            {
                Console.WriteLine($"{h.Id} - {h.Name} ({h.Unit})");
            }

            Console.WriteLine("-----------------------");

            if (!int.TryParse(Console.ReadLine(), out int habitId) ||
                !habits.Exists(h => h.Id == habitId))
            {
                Console.WriteLine("Invalid selection. Press any key to go back.");
                Console.ReadKey();
                return;
            }

            var selectedHabit = habits.Find(h => h.Id == habitId);
            LogMenu(selectedHabit);
        }

        static void LogMenu(Habits habit)
        {
            bool back = false;
            while (!back)
            {
                Console.Clear();
                Console.WriteLine($"MANAGE LOGS — {habit.Name} ({habit.Unit})");
                Console.WriteLine("0 - Back");
                Console.WriteLine("1 - View All Logs");
                Console.WriteLine("2 - Insert Log");
                Console.WriteLine("3 - Update Log");
                Console.WriteLine("4 - Delete Log");
                Console.WriteLine("-----------------------");

                switch (Console.ReadLine())
                {
                    case "0": back = true; break;
                    case "1": ViewAllLogs(habit); break;
                    case "2": Insert(habit); break;
                    case "3": Update(habit); break;
                    case "4": Delete(habit); break;
                    default:
                        Console.WriteLine("Invalid option. Press any key to try again.");
                        Console.ReadKey();
                        break;
                }
            }
        }

        // HABIT CRUD 

        private static void GetAllRecords()
        {
           
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                var tableCmd = connection.CreateCommand();
                tableCmd.CommandText = "SELECT Id, Name, Unit FROM habits";

                var habits = new List<Habits>();
                SqliteDataReader reader = tableCmd.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        habits.Add(new Habits
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Unit = reader.GetString(2)
                        });
                    }
                }
                else
                {
                    Console.WriteLine("No rows found");
                }

                connection.Close();
                Console.WriteLine("----------------------------------\n");
                foreach (var h in habits)
                    Console.WriteLine($"{h.Id} - {h.Name} | Unit: {h.Unit}");
                Console.WriteLine("-----------------------------------\n");
            }
            Console.ReadKey();
        }

        static void CreateHabit()
        {
            
            Console.Write("Enter habit name: ");
            string name = Console.ReadLine();

            Console.Write("Enter unit of measurement (e.g. glasses, km, pages): ");
            string unit = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(unit))
            {
                Console.WriteLine("Name and unit cannot be empty. Press any key to go back.");
                Console.ReadKey();
                return;
            }

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "INSERT INTO habits(Name, Unit) VALUES(@name, @unit)";
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@unit", unit);
                cmd.ExecuteNonQuery();
            }

            Console.WriteLine($"\nHabit '{name}' created! Press any key to continue.");
            Console.ReadKey();
        }

        internal static void UpdateHabit()
        {
            GetAllRecords();
            Console.Write("Enter the Id of the habit to update: ");

            if (!int.TryParse(Console.ReadLine(), out int id))
            {
                Console.WriteLine("Invalid Id. Press any key to go back.");
                Console.ReadKey();
                return;
            }

            Console.Write("New habit name: ");
            string name = Console.ReadLine();

            Console.Write("New unit of measurement: ");
            string unit = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(unit))
            {
                Console.WriteLine("Name and unit cannot be empty. Press any key to go back.");
                Console.ReadKey();
                return;
            }

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "UPDATE habits SET Name = @name, Unit = @unit WHERE Id = @id";
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@unit", unit);
                cmd.Parameters.AddWithValue("@id", id);
                int rows = cmd.ExecuteNonQuery();

                Console.WriteLine(rows == 0 ? "Habit not found." : "Habit updated!");
            }
            Console.ReadKey();
        }

        private static void DeleteHabit()
        {
            GetAllRecords();
            Console.Write("Enter the Id of the habit to delete (this will also delete all its logs): ");

            if (!int.TryParse(Console.ReadLine(), out int id))
            {
                Console.WriteLine("Invalid Id. Press any key to go back.");
                Console.ReadKey();
                return;
            }

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                var deleteLogsCmd = connection.CreateCommand();
                deleteLogsCmd.CommandText = "DELETE FROM habit_logs WHERE HabitId = @id";
                deleteLogsCmd.Parameters.AddWithValue("@id", id);
                deleteLogsCmd.ExecuteNonQuery();

                var deleteHabitCmd = connection.CreateCommand();
                deleteHabitCmd.CommandText = "DELETE FROM habits WHERE Id = @id";
                deleteHabitCmd.Parameters.AddWithValue("@id", id);
                int rows = deleteHabitCmd.ExecuteNonQuery();

                Console.WriteLine(rows == 0 ? "Habit not found." : "Habit and all its logs deleted!");
            }
            Console.ReadKey();
        }

        //LOG CRUD

        static void ViewAllLogs(Habits habit)
        {
            Console.Clear();
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText =
                    "SELECT Id, Date, Quantity FROM habit_logs WHERE HabitId = @habitId ORDER BY Date DESC";
                cmd.Parameters.AddWithValue("@habitId", habit.Id);

                var reader = cmd.ExecuteReader();
                Console.WriteLine("-----------------------");

                bool hasRows = false;
                while (reader.Read())
                {
                    hasRows = true;
                    Console.WriteLine($"Id: {reader.GetInt32(0)} | Date: {reader.GetString(1)} | {habit.Unit}: {reader.GetInt32(2)}");
                }

                if (!hasRows) Console.WriteLine("No logs found.");
                Console.WriteLine("-----------------------");
            }
            Console.ReadKey();
        }

        private static void Insert(Habits habit)
        {
            string date = GetDateInput();
            if (date == null) return;

            int quantity = GetNumberInput($"Enter quantity in {habit.Unit}:");
            if (quantity == -1) return;

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                var tableCmd = connection.CreateCommand();
                tableCmd.CommandText =
                    "INSERT INTO habit_logs(HabitId, Date, Quantity) VALUES(@habitId, @date, @quantity)";
                tableCmd.Parameters.AddWithValue("@habitId", habit.Id);
                tableCmd.Parameters.AddWithValue("@date", date);
                tableCmd.Parameters.AddWithValue("@quantity", quantity);
                tableCmd.ExecuteNonQuery();

                connection.Close();
            }

            Console.WriteLine("Log inserted! Press any key to continue.");
            Console.ReadKey();
        }

        private static void Delete(Habits habit)
        {
            Console.Clear();
            ViewAllLogs(habit);
            Console.Write("Enter the Id of the log to delete: ");

            if (!int.TryParse(Console.ReadLine(), out int id))
            {
                Console.WriteLine("Invalid Id. Press any key to go back.");
                Console.ReadKey();
                return;
            }

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText =
                    "DELETE FROM habit_logs WHERE Id = @id AND HabitId = @habitId";
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@habitId", habit.Id);
                int rows = cmd.ExecuteNonQuery();

                Console.WriteLine(rows == 0 ? "Log not found." : "Log deleted!");
            }
            Console.ReadKey();
        }

        internal static void Update(Habits habit)
        {
            ViewAllLogs(habit);
            Console.Write("Enter the Id of the log to update: ");

            if (!int.TryParse(Console.ReadLine(), out int id))
            {
                Console.WriteLine("Invalid Id. Press any key to go back.");
                Console.ReadKey();
                return;
            }

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                var checkCmd = connection.CreateCommand();
                checkCmd.CommandText =
                    "SELECT COUNT(*) FROM habit_logs WHERE Id = @id AND HabitId = @habitId";
                checkCmd.Parameters.AddWithValue("@id", id);
                checkCmd.Parameters.AddWithValue("@habitId", habit.Id);
                int exists = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (exists == 0)
                {
                    Console.WriteLine("Log not found. Press any key to go back.");
                    Console.ReadKey();
                    return;
                }

                string date = GetDateInput();
                if (date == null) return;

                int quantity = GetNumberInput($"Enter new quantity in {habit.Unit}:");
                if (quantity == -1) return;

                var cmd = connection.CreateCommand();
                cmd.CommandText =
                    "UPDATE habit_logs SET Date = @date, Quantity = @quantity WHERE Id = @id AND HabitId = @habitId";
                cmd.Parameters.AddWithValue("@date", date);
                cmd.Parameters.AddWithValue("@quantity", quantity);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@habitId", habit.Id);
                cmd.ExecuteNonQuery();

                Console.WriteLine("Log updated! Press any key to continue.");
                Console.ReadKey();
            }
        }

        //HELPERS 

        static List<Habits> GetAllHabits()
        {
            var habits = new List<Habits>();
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT Id, Name, Unit FROM habits";
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                    habits.Add(new Habits
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Unit = reader.GetString(2)
                    });
            }
            return habits;
        }

        internal static string GetDateInput()
        {
            Console.Write("\nPlease insert the date (Format: dd-MM-yy): ");
            string dateInput = Console.ReadLine();

            if (!DateTime.TryParseExact(dateInput, "dd-MM-yy", new CultureInfo("en-US"),
                    DateTimeStyles.None, out _))
            {
                Console.WriteLine("Invalid date format. Press any key to go back.");
                Console.ReadKey();
                return null;
            }

            return dateInput;
        }

        internal static int GetNumberInput(string message)
        {
            Console.Write(message + " ");
            if (!int.TryParse(Console.ReadLine(), out int result) || result < 0)
            {
                Console.WriteLine("Invalid number. Press any key to go back.");
                Console.ReadKey();
                return -1;
            }
            return result;
        }
    }

    public class Habits
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
    }

    public class habit_logs
    {
        public int ID { get; set; }
        public int HabitId { get; set; }
        public DateTime Date { get; set; }
        public int Quantity { get; set; }
    }
}