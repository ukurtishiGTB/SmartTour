using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SmartTour.ViewModels;
using SmartTour.Models;

namespace SmartTour.ViewModels
{
    public class TripCreateViewModel
    {
        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string Name { get; set; }

        [Required]
        [StringLength(1000)]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required]
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; } = DateTime.Today.AddDays(7);

        [Required]
        public List<PlaceInputViewModel> Places { get; set; } = new List<PlaceInputViewModel>();

        [Required]
        [Range(0, double.MaxValue)]
        [Display(Name = "Budget (USD)")]
        public decimal Budget { get; set; }
    }
} 