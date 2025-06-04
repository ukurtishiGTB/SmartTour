using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System;

namespace SmartTour.Models
{
    public class User
    {
        // This becomes the ArangoDB document's "_key"
        [JsonProperty("_key")]
        public string Key { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("email")]
        [EmailAddress]
        public string Email { get; set; }

        // New authentication properties
        [JsonProperty("password_hash")]
        public string PasswordHash { get; set; }

        [JsonProperty("location")]
        public string Location { get; set; }

        // An array of "tags" or "preferences"
        [JsonProperty("preferences")]
        public List<string> Preferences { get; set; } = new List<string>();

        [JsonProperty("preferred_duration")]
        public int PreferredDuration { get; set; } = 7;

        [JsonProperty("travel_style")]
        public string TravelStyle { get; set; } = "Balanced";

        [JsonProperty("budget_range")]
        public string BudgetRange { get; set; } = "Moderate";

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [JsonProperty("last_login")]
        public DateTime? LastLogin { get; set; }
    }

    // Add this class for login/registration forms
    public class UserLoginModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }

    public class UserRegistrationModel
    {
        [Required]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; }

        [Required]
        [Compare("Password")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }

        public List<string> Preferences { get; set; } = new List<string>();
    }
}