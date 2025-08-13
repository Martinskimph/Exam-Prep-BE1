using NUnit.Framework;
using RestSharp;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;

public class ApiResponseDTO
{
    public string Msg { get; set; }
    public string IdeaId { get; set; }
}

public class IdeaDTO
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string Url { get; set; }
}

public class UserAuthDTO
{
    public string Email { get; set; }
    public string Password { get; set; }
    public string AccessToken { get; set; }
}

public class UserCreateDTO
{
    public string UserName { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string RePassword { get; set; }
    public bool AcceptedAgreement { get; set; }
}

[TestFixture]
public class IdeaCenterTests
{[OneTimeTearDown]
public void TearDown()
{
    client.Dispose();
}
    private RestClient client;
    private string accessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiJjZGI3MWNkMS00YzI5LTQxMjctYWI3OC05OWZjMTU1OWY0ZmEiLCJpYXQiOiIwOC8xMy8yMDI1IDE2OjI3OjUyIiwiVXNlcklkIjoiNTFhNzQ5M2QtMjM3OC00MjQxLTkyYTEtMDhkZGI0OWRlYzE3IiwiRW1haWwiOiJpdm8xMjNAZ21haWwuY29tIiwiVXNlck5hbWUiOiJpdm8xIiwiZXhwIjoxNzU1MTI0MDcyLCJpc3MiOiJJZGVhQ2VudGVyX0FwcF9Tb2Z0VW5pIiwiYXVkIjoiSWRlYUNlbnRlcl9XZWJBUElfU29mdFVuaSJ9.-u9plSMG9NPctat4UF_CjX5WJqFDh1WNxhavShjP7PA";
    private static string lastCreatedIdeaId;

    private const string BaseUrl = "http://softuni-qa-loadbalancer-2137572849.eu-north-1.elb.amazonaws.com:84";
    private const string Email = "ivo123@gmail.com"; // Replace with your test email
    private const string Password = "123123"; // Replace with your test password

    [OneTimeSetUp]
    public void Setup()
    {
        // Initialize client
        client = new RestClient(BaseUrl);

        // Authenticate and get token
        var authRequest = new RestRequest("api/User/Authentication", Method.Post);
        authRequest.AddJsonBody(new UserAuthDTO
        {
            Email = Email,
            Password = Password
        });

        var authResponse = client.Execute<UserAuthDTO>(authRequest);
        accessToken = authResponse.Data.AccessToken;

        // Configure client to use the token for all requests
        client.AddDefaultHeader("Authorization", $"Bearer {accessToken}");
    }

    [Test, Order(1)]
    public void CreateNewIdea_WithRequiredFields_ShouldSucceed()
    {
        // Arrange
        var request = new RestRequest("api/Idea/Create", Method.Post);
        var idea = new IdeaDTO
        {
            Title = "Test Idea",
            Description = "This is a test idea description",
            Url = ""
        };
        request.AddJsonBody(idea);

        // Act
        var response = client.Execute<ApiResponseDTO>(request);
        request.AddJsonBody(idea);
        var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
        Assert.That(response.Data.Msg, Is.EqualTo("Successfully created!"));

        // Store the idea ID for later tests
        lastCreatedIdeaId = response.Data.IdeaId;
    }

    [Test, Order(2)]
    public void GetAllIdeas_ShouldReturnNonEmptyArray()
    {
        // Arrange
        var request = new RestRequest("api/Idea/All", Method.Get);

        // Act
        var response = client.Execute<List<IdeaDTO>>(request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
        Assert.That(response.Data, Is.Not.Empty);
    }

    [Test, Order(3)]
    public void EditLastCreatedIdea_ShouldSucceed()
    {
        var editRequest = new IdeaDTO
        {
            Title = "Edited Idea",
            Description = "This is an updated test idea description.",
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

    [Test, Order(4)]
    public void DeleteLastCreatedIdea_ShouldSucceed()
    {
        // Arrange
        var request = new RestRequest("api/Idea/Delete", Method.Delete);
        request.AddQueryParameter("ideaId", lastCreatedIdeaId);

        // Act
        var response = client.Execute(request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
        Assert.That(response.Content, Is.EqualTo("The idea is deleted!"));
    }

    [Test, Order(5)]
    public void CreateIdea_WithoutRequiredFields_ShouldFail()
    {
        // Arrange
        var request = new RestRequest("api/Idea/Create", Method.Post);
        var invalidIdea = new IdeaDTO
        {
            Title = "", // Missing required field
            Description = "", // Missing required field
            Url = ""
        };
        request.AddJsonBody(invalidIdea);

        // Act
        var response = client.Execute(request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.BadRequest));
    }

    [Test, Order(6)]
    public void EditNonExistingIdea_ShouldFail()
    {
        // Arrange
        var nonExistingId = "non-existing-id-123";
        var request = new RestRequest("api/Idea/Edit", Method.Put);
        request.AddQueryParameter("ideaId", nonExistingId);

        var idea = new IdeaDTO
        {
            Title = "Test",
            Description = "Test",
            Url = ""
        };
        request.AddJsonBody(idea);

        // Act
        var response = client.Execute(request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.BadRequest));
        Assert.That(response.Content, Is.EqualTo("There is no such idea!"));
    }

    [Test, Order(7)]
    public void DeleteNonExistingIdea_ShouldFail()
    {
        // Arrange
        var nonExistingId = "non-existing-id-123";
        var request = new RestRequest("api/Idea/Delete", Method.Delete);
        request.AddQueryParameter("ideaId", nonExistingId);

        // Act
        var response = client.Execute(request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.BadRequest));
        Assert.That(response.Content, Is.EqualTo("There is no such idea!"));
    }

    // Helper methods
    private void CreateTestUserIfNeeded()
    {
        var userRequest = new RestRequest("api/User/Create", Method.Post);
        userRequest.AddJsonBody(new UserCreateDTO
        {
            UserName = "testuser",
            Email = Email,
            Password = Password,
            RePassword = Password,
            AcceptedAgreement = true
        });

        var response = client.Execute(userRequest);

        if (response.StatusCode != System.Net.HttpStatusCode.OK)
        {
            Console.WriteLine("User creation failed - might already exist");
        }
    }

    private string AuthenticateAndGetToken()
    {
        var authRequest = new RestRequest("api/User/Authentication", Method.Post);
        authRequest.AddJsonBody(new { email = Email, password = Password });

        var response = client.Execute<UserAuthDTO>(authRequest);
        return response.Data.AccessToken;
    }
}