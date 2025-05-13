using Azure.Storage.Blobs;
using EventEaseAppOwethuHadebeMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Reflection.Metadata;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace EventEaseAppOwethuHadebeMVC.Controllers
{
    public class VenueController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VenueController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            
            return View(await _context.Venues.ToListAsync());
        }
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Venue venues)
        {
            if(ModelState.IsValid)
            {

                //Handle image upload to Azure Blob Storage if an image file was provided
                //Modify Controller to receive ImageFile from View (user upload)
                //Upload selected image to Azure Blob Storage
                if (venues.ImageFile != null)
                {
                    //upload Image to Blob Storage (Azure)
                    var blobUrl = await UploadImageToBlobAsync(venues.ImageFile);

                    //Save the Blob URL into ImageUrl property
                    venues.Image_Url = blobUrl;
                }
                _context.Add(venues);
                await _context.SaveChangesAsync();
                TempData["SuccessfulMessage"] = "Venue created successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(venues);
        }
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var venues = await _context.Venues.FindAsync(id);
            if (venues == null) return NotFound();

            return View(venues);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Venue venues)
        {
            if (id != venues.VenueID) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    if(venues.ImageFile != null)
                    {
                        //Upload new image if provided
                        var blobUrl = await UploadImageToBlobAsync(venues.ImageFile);

                        //Update Venue.ImageUrl with new Blob URL
                        venues.Image_Url = blobUrl;
                    }
                    else
                    {
                        //Keep the existing ImageUrl 
                    }
                    _context.Update(venues);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Venue updated successfully.";
                }
                catch(DbUpdateConcurrencyException)
                {
                    if (!VenueExists(venues.VenueID))
                        return NotFound();
                    else
                        throw;
                }
                    return RedirectToAction(nameof(Index));
            }
            return View(venues);
        }

        //Step1: Confirm Deletion (GET)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var venues = await _context.Venues.FirstOrDefaultAsync(v => v.VenueID == id);
            if (venues == null) return NotFound();

            return View(venues);
        }

        //Step 2: Perform Deletion (Post)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var venues = await _context.Venues.FindAsync(id);
            if(venues == null) return NotFound();

            var hasBookings = await _context.Bookings.AnyAsync(b => b.VenueID == id);
            if(hasBookings)
            {
                TempData["ErrorMessage"] = "Cannot delete venue because it has existing bookings.";
                return RedirectToAction(nameof(Index));
            }
            _context.Venues.Remove(venues);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Venue deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Details(int? id)
        {
            if(id == null) return NotFound();

            var venues = await _context.Venues.FirstOrDefaultAsync(m => m.VenueID == id);
            if (venues == null) return NotFound();

            return View(venues);
        }

        //Upload selected image to Azure Blob Storage
        //It completes the entire uploading process inside
        //This will upload the Image to Blob Storage Account
        //Uploads an image to Azure Blob Storage and returns the Blob URL

        private async Task<string> UploadImageToBlobAsync(IFormFile imageFile)
        {
            var connectionString = "DefaultEndpointsProtocol=https;AccountName=storagepoeparttwo;AccountKey=se9wx4dZtS/7r+rvdrUqUX93ejyRIN0iiLabKmexOy/hFYgf2b3O4/OxM4Ka68UjVZhV2CUYLKO9+AStrc0EUQ==;EndpointSuffix=core.windows.net";
            var containerName = "cldv6211poeparttwo";

            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            
            var blobClient = containerClient.GetBlobClient(Guid.NewGuid() + Path.GetExtension(imageFile.FileName));
            
            var blobHttpHeaders = new Azure.Storage.Blobs.Models.BlobHttpHeaders
            {
                ContentType = imageFile.ContentType
            };
            using (var stream = imageFile.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, new Azure.Storage.Blobs.Models.BlobUploadOptions
                {
                    HttpHeaders = blobHttpHeaders
                });
            }
            return blobClient.Uri.ToString();
        }

        //This is a small helper method
        //This small method will check if a Venue exists in your database.
        //Checks if a Venue exists

        private bool VenueExists(int id)
        {
            return _context.Venues.Any(e => e.VenueID == id);
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

    }
}
