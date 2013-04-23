using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace WebServerTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new TcpListener(IPAddress.Any, 5010); //create a server that will lsiten on all IP addr on port 5010
            server.Start();
            Console.WriteLine("Server started on port 5010");
            while (true)
            {
                Console.WriteLine("Waiting for client....");
                var client = server.AcceptTcpClient(); //a blocking function, we will WAIT for a tcp client to connect
                var clientData = client.GetStream();

                var buffer = new byte[1024]; //store clientData stream
                var bufferSize = clientData.Read(buffer, 0, 1024); //read into our buffer, Read returns an int of bytes recieved
                
                var requestString = Encoding.UTF8.GetString(buffer); //create a utf-8 string of the input
                requestString = requestString.Substring(0, bufferSize); //chop our string off at bufferSize
                Console.WriteLine(requestString);
                //resource holds the page the user requests, the page is located at the second word of the frist line of an
                //http request. (GET /index.html HTTP/1.1) so we split on the newline, get the first line, split on the space, skip the first word, and we skip the first / with substring
                var resource = requestString.Split('\n').First().Split(' ').Skip(1).First().Substring(1);
                string contents;
                if (resource == "")
                    resource = "index.html"; //when the root is requested, send index.html
                //we must parse our resource into a file path and the query string (?key=value&key2=value2)
                
                var parts = resource.Split('?');
                var file = parts[0]; //our file path will always be the first element
                var queryStringValues = new Dictionary<string, string>(); //this dictionary will hold our key,value pairs
                var fullPath = Path.Combine("Sites", file);
                if (parts.Length > 1) //only if a query is passed in will we process it
                {
                    var keyValuePairs = parts[1].Split('&'); //pairs are separated by an &
                    foreach (var keyValuePair in keyValuePairs)
                    {
                        var keyValueParts = keyValuePair.Split('=');
                        queryStringValues[keyValueParts[0]] = keyValueParts[1]; //add our key to the dictionary and assign its value
                    }
                }
                if (File.Exists(fullPath))
                {
                    using (var sr = new StreamReader(fullPath)) //serve up the requested file
                      contents = sr.ReadToEnd();

                    foreach (var key in queryStringValues.Keys) //replace any dynamic content with the value of our key,value pair
                    {
                        contents = contents.Replace(string.Format("?{0}", key), queryStringValues[key]);
                    }
                }
                 else
                    contents = "FILE NOT FOUND";

                
                var responseText = string.Format(@"
HTTP/1.1 200 OK
Date: {0}
Server: Test Server
Connection: close
Content-Type: text/html
Content-Length: {1}

{2}
", DateTime.Now.ToString("R"), Encoding.UTF8.GetBytes(contents).Length, contents); //prefacing a string with the @ symbol makes a literal string...it preserves formatting
               
                var responseBytes = Encoding.UTF8.GetBytes(responseText);
                clientData.Write(responseBytes, 0, responseBytes.Length);

                client.Close();
            }
        }
    }
}
