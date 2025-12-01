// ...existing code...
    public class CreateOrderDto
    {
        [Required]
        public string Address { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public string? Description { get; set; }

        public bool RequiresEvidence { get; set; }
    }
}

