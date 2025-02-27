using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Error-handling middleware
app.Use(async (context, next) =>
{
  try
  {
    await next.Invoke();
  }
  catch (Exception ex)
  {
    Console.WriteLine($"Exception: {ex.Message}");
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync("{\"error\": \"Internal server error.\"}");
  }
});

// Authentication middleware
app.Use(async (context, next) =>
{
  var token = context.Request.Headers["Authorization"].FirstOrDefault();

  if (string.IsNullOrEmpty(token) || token != "ValidToken")
  {
    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
    await context.Response.WriteAsync("Unauthorized");
    return;
  }

  await next.Invoke();
});

// Logging middleware
app.Use(async (context, next) =>
{
  var method = context.Request.Method;
  var path = context.Request.Path;

  await next.Invoke();

  var statusCode = context.Response.StatusCode;
  Console.WriteLine($"HTTP {method} {path} responded with {statusCode}");
});


// In-memory storage for users
var users = new List<User>();

// GET: Retrieve all users
app.MapGet("/users", () =>
{
  try
  {
    return Results.Ok(users);
  }
  catch (Exception ex)
  {
    return Results.Problem($"An error occurred: {ex.Message}");
  }
});

// GET: Retrieve a user by ID
app.MapGet("/users/{id}", (int id) =>
{
  try
  {
    var user = users.Find(u => u.Id == id);
    return user != null ? Results.Ok(user) : Results.NotFound("User not found");
  }
  catch (Exception ex)
  {
    return Results.Problem($"An error occurred: {ex.Message}");
  }
});

// POST: Add a new user
app.MapPost("/users", (User newUser) =>
{
  try
  {
    if (string.IsNullOrWhiteSpace(newUser.Name) || string.IsNullOrWhiteSpace(newUser.Email))
    {
      return Results.BadRequest("Name and Email are required.");
    }
    if (!newUser.Email.Contains("@"))
    {
      return Results.BadRequest("Invalid email format.");
    }

    users.Add(newUser);
    return Results.Created($"/users/{newUser.Id}", newUser);
  }
  catch (Exception ex)
  {
    return Results.Problem($"An error occurred: {ex.Message}");
  }
});

// PUT: Update an existing user's details
app.MapPut("/users/{id}", (int id, User updatedUser) =>
{
  try
  {
    var user = users.Find(u => u.Id == id);
    if (user == null)
    {
      return Results.NotFound("User not found");
    }

    if (string.IsNullOrWhiteSpace(updatedUser.Name) || string.IsNullOrWhiteSpace(updatedUser.Email))
    {
      return Results.BadRequest("Name and Email are required.");
    }
    if (!updatedUser.Email.Contains("@"))
    {
      return Results.BadRequest("Invalid email format.");
    }

    user.Name = updatedUser.Name;
    user.Email = updatedUser.Email;
    return Results.Ok(user);
  }
  catch (Exception ex)
  {
    return Results.Problem($"An error occurred: {ex.Message}");
  }
});

// DELETE: Remove a user by ID
app.MapDelete("/users/{id}", (int id) =>
{
  try
  {
    var user = users.Find(u => u.Id == id);
    if (user == null)
    {
      return Results.NotFound("User not found");
    }

    users.Remove(user);
    return Results.Ok("User deleted successfully");
  }
  catch (Exception ex)
  {
    return Results.Problem($"An error occurred: {ex.Message}");
  }
});

app.Run();

// User model class
public class User
{
  public int Id { get; set; }
  public required string Name { get; set; }
  public required string Email { get; set; }
}
