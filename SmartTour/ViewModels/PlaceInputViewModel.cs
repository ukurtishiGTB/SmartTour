using System.ComponentModel.DataAnnotations;

namespace SmartTour.ViewModels
{
    public class PlaceInputViewModel
    {
        [Required]
        public string Name { get; set; }
    }
} 