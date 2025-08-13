using System;
using System.Net;
using System.Text.Json;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using IdeaCentreExamPrep.Models;


namespace IdeaCentreExamPrep

{

    [TestFixture]

    public class IdeaCentreApiTests
    {
        private RestClient client;
        private string? lastCreatedIdeaId;

        private const string BaseUrl = "http://softuni-qa-loadbalancer-2137572849.eu-north-1.elb.amazonaws.com:84";

        private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiI2ZTMzNmJmNy02NjgwLTQwYjYtOTc5Yy01ZDJlYWVhMDU0YTUiLCJpYXQiOiIwOC8xMy8yMDI1IDEwOjA3OjQ5IiwiVXNlcklkIjoiMWYyYmM0NmMtMDhiOS00MTAxLWQyOWQtMDhkZGQ0ZTA4YmQ4IiwiRW1haWwiOiJ2YXNpMTIzMTIzQGV4YW1wbGUuY29tIiwiVXNlck5hbWUiOiJ2YXNpMTIzMTIzIiwiZXhwIjoxNzU1MTAxMjY5LCJpc3MiOiJJZGVhQ2VudGVyX0FwcF9Tb2Z0VW5pIiwiYXVkIjoiSWRlYUNlbnRlcl9XZWJBUElfU29mdFVuaSJ9.CxuUj3eRzXFj_KF1qS4YCILqXwamjAnpgLMsLqlPnaI";

        private const string LoginEmail = "vasi123123@example.com";
        private const string LoginPassword = "vasi123123";


        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken;

            if (!string.IsNullOrWhiteSpace(StaticToken))
            {
                jwtToken = StaticToken;
            }
            else
            {
                jwtToken = GetJwtToken(LoginEmail, LoginPassword);
            }

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };

            this.client = new RestClient(options);

        }

        private string GetJwtToken(string email, string password)
        {
            var tempClient = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { email, password });

            var response = tempClient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accessToken").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Failed to retreive JWT token from response");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status code: {response.Content}, Content {response.Content}");
            }


        }


        [Order(1)]
        [Test]

        public void CreateIdea_WithRequiredFields_ShoulReturnSuccess()
        {
            var ideaRequest = new IdeaDTO
            {
                Title = "Test idea",
                Description = "This is the description for test idea",
                Url = ""
            };

            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(ideaRequest);
            var response = this.client.Execute(request);
            var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(createResponse.Msg, Is.EqualTo("Successfully created!"));

            // In CreateIdea_WithRequiredFields_ShoulReturnSuccess
            lastCreatedIdeaId = createResponse.Id;
        }

        [Order(2)]
        [Test]

        public void GetAllIdeas_ShouldReturnListOfIdeas()
        {
            var request = new RestRequest("/api/Idea/All", Method.Get);
            var response = this.client.Execute(request);
            var responseItems = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(responseItems, Is.Not.Null);
            Assert.That(responseItems, Is.Not.Empty);


            lastCreatedIdeaId = responseItems.LastOrDefault()?.Id;

        }


        [Order(3)]
        [Test]

        public void EditExistingIdea_ShouldReturnSuccess()
        {
            var editRequest = new IdeaDTO
            {
                Title = "Edited idea",
                Description = "This is the edited idea",
                Url = ""
            };

            var request = new RestRequest($"/api/Idea/Edit", Method.Put);
            request.AddQueryParameter("ideaId", lastCreatedIdeaId);
            request.AddJsonBody(editRequest);

            var response = this.client.Execute(request);
            var editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(editResponse.Msg, Is.EqualTo("Edited successfully"));

        }

        [Order(4)]
        [Test]
        public void DeleteIdea_ShouldReturnSuccess()
        {
            var request = new RestRequest($"/api/Idea/Delete", Method.Delete);
            request.AddQueryParameter("ideaId", lastCreatedIdeaId);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("The idea is deleted!"));
        }


        [Order(5)]
        [Test]

        public void CreateIdea_WithMissingFields_ShouldReturnSuccessAgain()
        {
            var ideaRequest = new IdeaDTO
            {
                Title = "",
                Description = ""
            };
           
            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(ideaRequest);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        }

        [Order(6)]
        [Test]

        public void EditNonExistingIdea_ShouldReturnBadRequest()
        {
            var editRequest = new IdeaDTO
            {
                Title = "Non-existing idea",
                Description = "This idea does not exist",
                Url = ""
            };
            var request = new RestRequest("/api/Idea/Edit", Method.Put);
            request.AddQueryParameter("ideaId", "non-existing-id");
            request.AddJsonBody(editRequest);
            var response = this.client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("There is no such idea!"));

        }

        [Order(7)]
        [Test]

        public void DeleteNonExistingIdea_ShouldReturnBadRequest()
        {
            var request = new RestRequest("/api/Idea/Delete", Method.Delete);
            request.AddQueryParameter("ideaId", "non-existing-id");
            var response = this.client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("There is no such idea!"));

        }

















        [OneTimeTearDown]

        public void Teardown()
        {
            this.client.Dispose();
        }


    }
}