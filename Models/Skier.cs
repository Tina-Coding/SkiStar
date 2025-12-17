using Microsoft.Extensions.Configuration;
using SkiDataSimulator.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkiDataSimulator.Models
{
    public class Skier
    {
        


        public int Id { get; set; }
        public string? Firstname { get; set; }
        public string? Lastname { get; set; }
        public string Email { get; set; }
        public string Fullname => $"{Firstname} {Lastname}";




    } 



    }
