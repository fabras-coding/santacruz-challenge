using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace StaCruzChallenge.Application.Products
{
    public record ProductDto
    {

        public Guid Id {get;set;} 

        [Required]
        public string? Name {get;set;}   
        public string? Description {get;set;}

        [Required(ErrorMessage = "Price is required"),Range(0.01,double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price {get;set;}
    }
}