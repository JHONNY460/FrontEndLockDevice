using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System;
using MySql.Data.MySqlClient; //my sql connection manager
using System.Linq; //used to use sql queries
using Org.BouncyCastle.Crypto; //Encryption
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using System.IO;

class Database
{
    private readonly string _connectionString; //variable to hold the connection string for SQL connection

    public Database(string server, string database, string userId, string password) // create a Database class member
    {
        _connectionString = $"Server={server};Database={database};User Id={userId};Password={password};"; // set the connection string
    }

    public async Task<(bool isAuthenticated, string username)> AuthenticateUserAsync(string username, string password) //login function - getting 2 strings username and password
    {
        using (var httpClient = new HttpClient()) //initialized http client to call the js server
        {
            // Create the request body with username and password
            var requestBody = new
            {
                email = username, // Use the username parameter
                password = password  // Use the password parameter
            };

            // Serialize the request body to JSON
            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Send the POST request to the signin endpoint
            var response = await httpClient.PostAsync("http://localhost:3000/signin", content);

            // Log the response status code
            Console.WriteLine($"Response Status Code: {response.StatusCode}");

            // Read the response content
            var responseContent = await response.Content.ReadAsStringAsync();

            // Log the response content
            Console.WriteLine($"Response Content: {responseContent}");

            // Check if the response status code is 200 (success)
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                // Deserialize the response content to get the username
                var responseObject = JsonSerializer.Deserialize<SignInResponse>(responseContent);

                // Return true and the username
                return (true, requestBody.email);
            }
        }

        // Return false and an empty username if authentication fails
        return (false, string.Empty);
    }

    // Define a class to match the structure of the JSON response
    public class SignInResponse
    {
        public string username { get; set; } // Assuming the response contains the username
    }


private void SendLockNotification(Device device) // the function we using to send the application a message of "lock"
{
    try
    {
        string message = "Your account has been locked."; //setting the message
        byte[] data = Encoding.UTF8.GetBytes(message);

            Console.WriteLine(AppDomain.CurrentDomain.BaseDirectory, "public_key.pem");
            string publicKeyFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "public_key.pem");
            AsymmetricKeyParameter publicKey = GetPublicKeyFromFile(publicKeyFilePath);

            // Create an RSA engine with PKCS1 padding
            var cipher = new Pkcs1Encoding(new RsaEngine());
        cipher.Init(true, publicKey); // true for encryption

        byte[] encryptedData = cipher.ProcessBlock(data, 0, data.Length); // Encrypt the data

            
            device.Connection.Send(encryptedData); // Send the encrypted data to the app by using Device class member
            Console.WriteLine($"Sent encrypted lock notification to {device.Username} ({device.IP})"); // log we locked the device on the console
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error sending lock notification to {device.Username}: {ex.Message}");  //writing when there an error
    }
}

    // Get or generate a public key for the device
    private AsymmetricKeyParameter GetPublicKeyFromFile(string filePath)
    {
        using (var reader = File.OpenText(filePath))
        {
            PemReader pemReader = new PemReader(reader);
            object keyObject = pemReader.ReadObject();

            if (keyObject is AsymmetricCipherKeyPair)
            {
                // If the file contains a key pair, extract the public key
                return ((AsymmetricCipherKeyPair)keyObject).Public;
            }
            else if (keyObject is RsaKeyParameters)
            {
                // If the file contains only the public key
                return (AsymmetricKeyParameter)keyObject;
            }
            else
            {
                throw new InvalidOperationException("Invalid key format in file.");
            }
        }
    }
    public async Task<List<string>> GetUsersToLockAsync(List<Device> connectedDevices) // running each 10 sec's to see if there device we should lock
    {
        Console.WriteLine($"Test lock is running?"); // log to know if it run
        var usersToLock = new List<string>(); // creating an empty list to fill with data of devices we need to lock

        using (var connection = new MySqlConnection(_connectionString))
        {
            await connection.OpenAsync(); // creating MySql connection

            // שליפת המשתמשים שצריך לנעול
            var query = "SELECT email, username FROM users WHERE should_lock = 1";
            using (var command = new MySqlCommand(query, connection))
            {
                using (var reader = await command.ExecuteReaderAsync()) //running the query
                {
                    int emailIndex = reader.GetOrdinal("email");
                    int usernameIndex = reader.GetOrdinal("username");

                    while (await reader.ReadAsync())
                    {
                        string email = reader.GetString(emailIndex); //setting variables from query results
                        string username = reader.GetString(usernameIndex); //setting variables from query results
                        usersToLock.Add(email); // adding emails of the users we want to lock

                        // שליחת הודעה למכשיר של המשתמש אם קיים ברשימת המכשירים המחוברים
                        var device = connectedDevices.FirstOrDefault(d => d.Username == email); // searching the device by the emails we saved earlier
                        if (device != null) // if its not null - mean we got a device on the list.
                        {
                            Console.WriteLine("Trying to lock device."); // console inform of action.
                            SendLockNotification(device); // calling the lock function with the device info.
                        }
                    }
                }
            }



            // עדכון המשתמשים שנמצאו ל-should_lock = FALSE
            if (usersToLock.Any()) 
            {
                var updateQuery = "UPDATE users SET should_lock = FALSE WHERE email = @email";
                using (var updateCommand = new MySqlCommand(updateQuery, connection))
                {
                    foreach (var email in usersToLock)
                    {
                        updateCommand.Parameters.Clear();
                        updateCommand.Parameters.AddWithValue("@email", email);
                        await updateCommand.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        return usersToLock;
    }
}