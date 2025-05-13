using EventEaseAppOwethuHadebeMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventEaseAppOwethuHadebeMVC.Controllers
{
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index(string searchString)
        {
            var bookings = _context.Bookings
                .Include(b => b.Venue)
                .Include(b => b.Event)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                bookings = bookings.Where(b =>
                b.Venue.VenueName.Contains(searchString) ||
                b.Event.EventName.Contains(searchString));
            }

            return View(await bookings.ToListAsync());
        }
        public IActionResult Create()
        {
            LoadVenueAndEventData();
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Booking bookings)
        {
            var selectedEvent = await _context.Events.FirstOrDefaultAsync(e => e.EventID == bookings.EventID);

            if (selectedEvent == null)
            {
                ModelState.AddModelError("", "Selected event not found");
                LoadVenueAndEventData();
                return View(bookings);
            }

            // Check for conflicts: either same venue or same event on the same date
            var isConflict = await _context.Bookings
                .AnyAsync(b =>
                    (b.VenueID == bookings.VenueID || b.EventID == bookings.EventID) &&
                    b.Event.EventDate.Date == selectedEvent.EventDate.Date);

            if (isConflict)
            {
                ModelState.AddModelError("", "A booking with the same venue or event already exists on the selected date.");
                LoadVenueAndEventData();
                return View(bookings);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(bookings);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Booking created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "An error occurred while creating the booking.");
                    LoadVenueAndEventData();
                    return View(bookings);
                }
            }

            LoadVenueAndEventData();
            return View(bookings);
        }


        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var bookings = await _context.Bookings.FindAsync(id);
            if (bookings == null) return NotFound();

            LoadVenueAndEventData();
            return View(bookings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Booking bookings)
        {
            if (id != bookings.BookingID) return NotFound();

            var selectedEvent = await _context.Events.FirstOrDefaultAsync(e => e.EventID == bookings.EventID);
            if (selectedEvent == null)
            {
                ModelState.AddModelError("", "Selected event not found");
                LoadVenueAndEventData();
                return View(bookings);
            }

            // Check for conflicts: either same venue or same event on the same date, excluding the current booking
            var isConflict = await _context.Bookings
                .AnyAsync(b =>
                    b.BookingID != id &&
                    (b.VenueID == bookings.VenueID || b.EventID == bookings.EventID) &&
                    b.Event.EventDate.Date == selectedEvent.EventDate.Date);

            if (isConflict)
            {
                ModelState.AddModelError("", "A booking with the same venue or event already exists on the selected date.");
                LoadVenueAndEventData();
                return View(bookings);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(bookings);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Booking updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookingExists(bookings.BookingID))
                        return NotFound();
                    else
                        throw;
                }
            }

            LoadVenueAndEventData();
            return View(bookings);
        }

        private void LoadVenueAndEventData()
        {
            ViewData["Venues"] = _context.Venues.ToList();
            ViewData["Events"] = _context.Events.ToList();
        }
        // Validate double booking
        public async Task<IActionResult> ValidateDoubleBooking(int venueId, DateTime eventDate)
        {
            var hasConflict = await _context.Bookings
                .AnyAsync(b => b.VenueID == venueId && b.Event.EventDate.Date == eventDate.Date);

            if (hasConflict)
            {
                TempData["ErrorMessage"] = "This venue is already booked for the selected date.";
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = "The venue is available for the selected date.";
            return RedirectToAction(nameof(Index));
        }
        private bool BookingExists(int id)
        {
            return _context.Bookings.Any(e => e.BookingID == id);
        }


    }
}

