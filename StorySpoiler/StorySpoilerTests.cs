using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using StorySpoiler.Models;
using System.Net;
using System.Text.Json;


namespace StorySpoiler
{
    [TestFixture]
    public class StorySpoilerTests
    {
        private RestClient client;
        private static string createdStoryId;
        private const string baseUrl = "https://d3s5nxhwblsjbi.cloudfront.net";

        [OneTimeSetUp]
        public void Setup()
        {
            
            string token = GetJwtToken("emreg92", "123456");

            var options = new RestClientOptions(baseUrl)
            {
                Authenticator = new JwtAuthenticator(token)
            };

            client = new RestClient(options);
        }

        private string GetJwtToken(string username, string password)
        {
            RestClient tempClient = new RestClient(baseUrl);
            var tempRequest = new RestRequest("/api/User/Authentication");
            tempRequest.AddJsonBody(new { username, password });

            var response = tempClient.Execute(tempRequest, Method.Post);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = responseContent.GetProperty("accessToken").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Token is null or empty.");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Authentication failed: {response.StatusCode} with data {response.Content}");
            }
        }

       

        [Test, Order(1)]
        public void CreateStory_WithRequiredFields_ShouldReturnSuccessMessageAndStoryId()
        {
            
            var story = new StoryDTO
            {
                Title = "Test Story",
                Description = "This is a test story description.",
                Url = ""
            };

            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(story);

            var response = client.Execute(request);
            var json = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
           
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.IsNotEmpty(json.StoryId, "Story ID should not be empty.");
            Assert.That(json.Msg, Is.EqualTo("Successfully created!"));


            createdStoryId = json.StoryId;

        }

        [Test, Order(2)]

        public void EditStory_WithExistingStoryId_ShouldReturnSuccessMessage()
        {
            
            var editedStory = new StoryDTO ()
            {
                Title = "Updated Test Story",
                Description = "This is an updated test story description.",
                Url = ""
            };

            var request = new RestRequest($"/api/Story/Edit/{createdStoryId}", Method.Put);
            request.AddJsonBody(editedStory);

            var response = client.Execute(request);
            var json = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(json.Msg, Is.EqualTo("Successfully edited"));



        }

        [Test, Order(3)]

        public void GetAllStories_ShouldReturnNonEmptyArray()
        {
            var request = new RestRequest("/api/Story/All", Method.Get);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code OK (200).");

            var stories = JsonSerializer.Deserialize<ApiResponseDTO[]>(response.Content);
            Assert.That(stories, Is.Not.Empty);
        }

        [Test, Order(4)]

        public void DeleteStory_ShoudReturnSuccessMessage()
        {
            var request = new RestRequest($"/api/Story/Delete/{createdStoryId}", Method.Delete);

            var response = client.Execute(request);
            var json = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(json.Msg, Is.EqualTo("Deleted successfully!"));

        }

        [Test, Order(5)]

        public void CreateStory_WithoutRequiredFields_ShouldReturnBadRequest()
        {
            var story = new StoryDTO
            {
                Title = "",
                Description = "",
                Url = ""
            };

            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(story);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }


        [Test, Order(6)]

        public void EditNonExistingStory_ShouldReturnNotFound()
        {
            var editedStory = new StoryDTO
            {
                Title = "Non-existing Story",
                Description = "This story does not exist.",
                Url = ""
            };

            var request = new RestRequest("/api/Story/Edit/non-existing-id", Method.Put);
            request.AddJsonBody(editedStory);

            var response = client.Execute(request);
            var json = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(json.Msg, Is.EqualTo("No spoilers..."));


        }

        [Test, Order(7)]

        public void DeleteNonExistingStory_ShouldReturnBadRequest()
        {
            var request = new RestRequest("/api/Story/Delete/non-existing-id", Method.Delete);

            var response = client.Execute(request);
            var json = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(json.Msg, Is.EqualTo("Unable to delete this story spoiler!"));
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            client?.Dispose();
        }
    }
}