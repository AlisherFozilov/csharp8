using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Data;

namespace _8
{
    class Program
    {
        static void Main(string[] args)
        {
            var connectionStr =
            @"Server=127.0.0.1,1433; 
            Database=Master; 
            User Id=SA; 
            password=Your_password123";

            using (var personRepo = new PersonRepo(connectionStr))
            {
                while (true)
                {
                    try
                    {
                        menuLoopStart(personRepo);
                    }
                    catch (BadInput)
                    {
                        Console.WriteLine("Bad input. Please, try again");
                    }
                    catch (NoRows)
                    {
                        Console.WriteLine("not found");
                    }
                    catch (SqlTypeException)
                    {
                        Console.WriteLine("Bad input, can not put data to db, check your input");
                    }
                    catch (Exit)
                    {
                        break;
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Unknown Internal Error");
                        return;
                    }
                }
            }
        }

        static void menuLoopStart(PersonRepo personRepo)
        {
            const string Instructions =
                @"Choose operation:
                1. Insert
                2. Select All
                3. Select
                4. Update 
                5. Delete

                0. Exit
                ";

            Console.WriteLine("===================================================");
            Console.WriteLine();
            Console.WriteLine(Instructions);
            var ok = Int32.TryParse(Console.ReadLine(), out var input);
            if (!ok)
            {
                throw new BadInput("bad input");
            }

            switch (input)
            {
                case 0:
                    throw new Exit();
                case 1:
                    {
                        var p = enterPerson();
                        personRepo.Insert(p);
                    }
                    break;
                case 2:
                    var records = personRepo.SelectAll();
                    foreach (var item in records)
                    {
                        Console.WriteLine(item);
                    }
                    break;
                case 3:
                    {
                        Console.WriteLine("Enter id:");
                        var successful = Int32.TryParse(Console.ReadLine(), out var id);
                        if (!successful)
                        {
                            throw new BadInput("bad input");
                        }

                        var p = personRepo.SelectByID(id);
                        Console.WriteLine(p);
                    }
                    break;
                case 4:
                    {
                        Console.WriteLine("Enter id:");
                        var isOk = Int32.TryParse(Console.ReadLine(), out var id);
                        if (!isOk)
                        {
                            throw new BadInput("bad input");
                        }
                        var p = enterPerson();
                        p.ID = id;
                        personRepo.Update(p);
                    }
                    break;
                case 5:
                    {
                        Console.WriteLine("Enter id:");
                        var successful = Int32.TryParse(Console.ReadLine(), out var id);
                        if (!successful)
                        {
                            throw new BadInput("bad input");
                        }

                        personRepo.Delete(id);
                    }
                    break;
                default:
                    throw new BadInput("bad input");
            }
        }

        static Person enterPerson()
        {
            var p = new Person();
            Console.WriteLine("Enter person info");
            Console.WriteLine("LastName:");
            p.LastName = Console.ReadLine();
            Console.WriteLine("FirstName:");
            p.FirstName = Console.ReadLine();
            Console.WriteLine("Do person have middle name?(y/n)");
            var answer = Console.ReadLine();
            if (answer == "y")
            {
                Console.WriteLine("MiddleName:");
                p.MiddleName = Console.ReadLine();
            }
            Console.WriteLine("BirthDate (2000.10.15 / YY.MM.DD)");
            var ok = DateTime.TryParse(Console.ReadLine(), out var date);
            if (!ok)
            {
                throw new BadInput("bad input");
            }
            p.BirthDate = date;
            return p;
        }

        static void temp()
        {
            var connectionStr =
            @"Server=127.0.0.1,1433; 
            Database=Master; 
            User Id=SA; 
            password=Your_password123";

            var personRepo = new PersonRepo(connectionStr);

            personRepo.Insert(new Person()
            {
                LastName = "Bennington",
                FirstName = "Chester",
                MiddleName = null,
                BirthDate = new DateTime(1989, 10, 5),
            });

            var records = personRepo.SelectAll();
            foreach (var item in records)
            {
                Console.WriteLine(item);
            }
            const int id = 6;
            var p = personRepo.SelectByID(id);
            Console.WriteLine(p);

            p.FirstName = "Cheesy";
            personRepo.Update(p);

            p = personRepo.SelectByID(id);
            Console.WriteLine(p);

            personRepo.Delete(id);

            try
            {
                p = personRepo.SelectByID(id);
                Console.WriteLine(p);
            }
            catch (NoRows)
            {
                Console.WriteLine("no record with id ", id);
            }
        }

    }

    class Person
    {
        public long ID { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public DateTime BirthDate { get; set; }

        override public string ToString()
        {
            return $"{ID} {LastName} {FirstName} {MiddleName} {BirthDate}";
        }
    }

    class PersonRepo : IDisposable
    {
        private string connectionStr { get; set; }
        private SqlConnection connection { get; set; }

        public void Dispose()
        {
            Console.WriteLine("***************************************");
            connection.Close();
        }
        public PersonRepo(string connectionStr)
        {
            Console.WriteLine("PersonRepo");
            this.connection = new SqlConnection(connectionStr);
            connection.Open();
            if (this.connection.State != ConnectionState.Open)
            {
                throw new Exception("db connected unsuccessfully");
            }
        }

        public void Insert(Person p)
        {
            const string query = @"INSERT INTO Person (LastName, FirstName, MiddleName, BirthDate)
VALUES (@LastName, @FirstName, @MiddleName, @BirthDate)";

            var middleName = String.IsNullOrWhiteSpace(p.MiddleName) ? (Object)DBNull.Value : p.MiddleName;
            var command = new SqlCommand(query, this.connection);

            command.Parameters.AddWithValue("@LastName", p.LastName);
            command.Parameters.AddWithValue("@FirstName", p.FirstName);
            command.Parameters.AddWithValue("@MiddleName", middleName);
            command.Parameters.AddWithValue("@BirthDate", p.BirthDate);
            command.ExecuteNonQuery();
        }

        public List<Person> SelectAll()
        {
            const string query = @"SELECT Id, LastName, FirstName, MiddleName, BirthDate FROM Person";
            var command = new SqlCommand(query, this.connection);
            var reader = command.ExecuteReader();
            var list = new List<Person>();

            try
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        var p = new Person();

                        p.ID = reader.GetInt32(0);
                        p.LastName = reader.GetString(1);
                        p.FirstName = reader.GetString(2);
                        p.MiddleName = reader.GetSqlString(3).IsNull ? null : reader.GetSqlString(3).ToString();
                        p.BirthDate = reader.GetDateTime(4);

                        list.Add(p);
                    }
                }
                else
                {
                    throw new NoRows("no rows");
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                reader.Close();
            }

            return list;
        }

        public Person SelectByID(long id)
        {
            const string query = @"SELECT Id, LastName, FirstName, MiddleName, BirthDate
FROM Person
WHERE Id = @Id";
            var command = new SqlCommand(query, this.connection);
            command.Parameters.AddWithValue("@Id", id);
            var reader = command.ExecuteReader();
            var p = new Person();

            try
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        p.ID = reader.GetInt32(0);
                        p.LastName = reader.GetString(1);
                        p.FirstName = reader.GetString(2);
                        p.MiddleName = reader.GetSqlString(3).IsNull ? null : reader.GetSqlString(3).ToString();
                        p.BirthDate = reader.GetDateTime(4);
                    }
                }
                else
                {
                    throw new NoRows("no rows");
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                reader.Close();
            }

            return p;
        }

        public void Update(Person p)
        {
            var middleName = String.IsNullOrWhiteSpace(p.MiddleName) ? (Object)DBNull.Value : p.MiddleName;
            const string query = @"UPDATE Person
SET LastName = @LastName,
    FirstName = @FirstName,
    MiddleName = @MiddleName,
    BirthDate = @BirthDate
WHERE Id = @Id;";
            var command = new SqlCommand(query, this.connection);

            command.Parameters.AddWithValue("@LastName", p.LastName);
            command.Parameters.AddWithValue("@FirstName", p.FirstName);
            command.Parameters.AddWithValue("@MiddleName", middleName);
            command.Parameters.AddWithValue("@BirthDate", p.BirthDate);
            command.Parameters.AddWithValue("@Id", p.ID);
            var rowsAffected = command.ExecuteNonQuery();
            if (rowsAffected < 1)
            {
                throw new NoRows();
            }
        }

        public void Delete(long id)
        {
            const string q = @"DELETE FROM Person WHERE Id = @Id";
            var command = new SqlCommand(q, this.connection);
            command.Parameters.AddWithValue("@Id", id);
            var rowsAffected = command.ExecuteNonQuery();
            if (rowsAffected < 1)
            {
                throw new NoRows();
            }
        }
    }

    [System.Serializable]
    public class NoRows : System.Exception
    {
        public NoRows() { }
        public NoRows(string message) : base(message) { }
        public NoRows(string message, System.Exception inner) : base(message, inner) { }
        protected NoRows(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}

[System.Serializable]
public class BadInput : System.Exception
{
    public BadInput() { }
    public BadInput(string message) : base(message) { }
    public BadInput(string message, System.Exception inner) : base(message, inner) { }
    protected BadInput(
        System.Runtime.Serialization.SerializationInfo info,
        System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}

[System.Serializable]
public class Exit : System.Exception
{
    public Exit() { }
    public Exit(string message) : base(message) { }
    public Exit(string message, System.Exception inner) : base(message, inner) { }
    protected Exit(
        System.Runtime.Serialization.SerializationInfo info,
        System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}