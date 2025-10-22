using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using wcerms.Data;
using wcerms.Models;
using System.Linq;

namespace wcerms.Pages.Organizer
{
    public class DashboardModel : PageModel
    {
        private readonly AppDbContext _context;

        public DashboardModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)] public string Section { get; set; } = "Overview";
        [BindProperty] public string EventName { get; set; } = "";
        [BindProperty] public DateTime EventDate { get; set; } = DateTime.Today;
        [BindProperty] public string? EventDescription { get; set; }
        [BindProperty] public string VenueName { get; set; } = "";
        [BindProperty] public string? VenueDetails { get; set; }
        [BindProperty] public string ResourceType { get; set; } = "";
        [BindProperty] public int Quantity { get; set; }

        public List<Venue>? AvailableVenues { get; set; }
        public List<BookingViewModel>? MyBookings { get; set; }

        public int TotalEvents { get; set; }
        public int TotalVenues { get; set; }
        public int TotalResources { get; set; }
        public int TotalBookings { get; set; }

        public void OnGet()
        {
            if (Section == "Overview")
            {
                TotalEvents = _context.Events.Count();
                TotalVenues = _context.Venues.Count();
                TotalResources = _context.Resources.Count();
                TotalBookings = _context.Bookings.Count();
            }
            else if (Section == "CreateEvent")
            {
                AvailableVenues = _context.Venues.Where(v => v.Status == "Approved").ToList();
            }
            else if (Section == "MyBookings")
            {
                LoadBookings();
            }
        }

        public IActionResult OnPostCreateEvent()
        {
            if (EventDate < DateTime.Today)
            {
                TempData["Message"] = "Event date cannot be in the past.";
                return RedirectToPage("/Organizer/Dashboard", new { Section = "CreateEvent" });
            }

            if (string.IsNullOrWhiteSpace(EventName))
            {
                TempData["Message"] = "Please enter an event name.";
                return RedirectToPage("/Organizer/Dashboard", new { Section = "CreateEvent" });
            }

            var newEvent = new Event
            {
                Title = EventName,
                Description = EventDescription,
                start_datetime = EventDate,
                end_datetime = EventDate.AddHours(2),
                organizer_id = _context.Users.FirstOrDefault()
            };

            _context.Events.Add(newEvent);
            _context.SaveChanges();

            TempData["Message"] = $"Event '{EventName}' created successfully!";
            return RedirectToPage("/Organizer/Dashboard", new { Section = "CreateEvent" });
        }

        public IActionResult OnPostRequestVenue()
        {
            if (string.IsNullOrWhiteSpace(VenueName))
            {
                TempData["Message"] = "Venue name is required.";
                return RedirectToPage("/Organizer/Dashboard", new { Section = "RequestVenue" });
            }

            var venue = new Venue
            {
                Name = VenueName,
                Details = VenueDetails,
                Capacity = 0,
                Location = "To be confirmed",
                Status = "Pending"
            };

            _context.Venues.Add(venue);
            _context.SaveChanges();

            TempData["Message"] = $"Venue '{VenueName}' request submitted!";
            return RedirectToPage("/Organizer/Dashboard", new { Section = "RequestVenue" });
        }

        public IActionResult OnPostRequestResources()
        {
            if (string.IsNullOrWhiteSpace(ResourceType) || Quantity <= 0)
            {
                TempData["Message"] = "Please provide valid resource and quantity.";
                return RedirectToPage("/Organizer/Dashboard", new { Section = "RequestResources" });
            }

            var resource = new Resource
            {
                ResourceType = ResourceType,
                ResourceName = $"{ResourceType} Request",
                Quantity = Quantity,
                Status = "Pending"
            };

            _context.Resources.Add(resource);
            _context.SaveChanges();

            TempData["Message"] = $"Resource request for '{ResourceType}' submitted!";
            return RedirectToPage("/Organizer/Dashboard", new { Section = "RequestResources" });
        }

        private void LoadBookings()
        {
            var query = from b in _context.Bookings
                        join e in _context.Events on b.EventId equals e.EventId
                        join v in _context.Venues on b.VenueId equals v.VenueId
                        select new BookingViewModel
                        {
                            EventName = e.Title,
                            VenueName = v.Name,
                            Date = b.Date,
                            Status = b.Status
                        };

            MyBookings = query.OrderByDescending(b => b.Date).ToList();

            foreach (var booking in MyBookings)
            {
                if (booking.Date < DateTime.Today && booking.Status != "Completed")
                    booking.Status = "Completed";
            }
        }

        public class BookingViewModel
        {
            public string EventName { get; set; } = "";
            public string VenueName { get; set; } = "";
            public DateTime Date { get; set; }
            public string Status { get; set; } = "";
        }
    }
}
