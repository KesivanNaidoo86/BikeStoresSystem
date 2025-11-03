using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using BikeStoresSystem.ViewModels;

namespace BikeStoresSystem.Controllers
{
    public class HomeController : Controller
    {
        private BikeStoresEntities db = new BikeStoresEntities();

        public async Task<ActionResult> Index(int? brandFilter, int? categoryFilter)
        {
            ViewBag.Brands = await db.brands.ToListAsync();
            ViewBag.Categories = await db.categories.ToListAsync();
            ViewBag.SelectedBrand = brandFilter;
            ViewBag.SelectedCategory = categoryFilter;

            var productsQuery = db.products
                .Include(p => p.brand)
                .Include(p => p.category)
                .AsQueryable();

            if (brandFilter.HasValue && brandFilter.Value > 0)
            {
                productsQuery = productsQuery.Where(p => p.brand_id == brandFilter.Value);
            }

            if (categoryFilter.HasValue && categoryFilter.Value > 0)
            {
                productsQuery = productsQuery.Where(p => p.category_id == categoryFilter.Value);
            }

            var productList = await productsQuery.ToListAsync();
            ViewBag.Products = productList.Select(p => new ProductViewModel
            {
                product_id = p.product_id,
                product_name = p.product_name,
                brand_id = p.brand_id,
                category_id = p.category_id,
                model_year = p.model_year,
                list_price = p.list_price,
                brand_name = p.brand != null ? p.brand.brand_name : "Unknown",
                category_name = p.category != null ? p.category.category_name : "Unknown"
            }).ToList();

            var allStaff = await db.staffs.Include(s => s.store).ToListAsync();

            var staffViewModels = new List<StaffViewModel>();
            foreach (var s in allStaff)
            {
                var managerName = "N/A";
                if (s.manager_id.HasValue)
                {
                    var manager = allStaff.FirstOrDefault(m => m.staff_id == s.manager_id.Value);
                    if (manager != null)
                    {
                        managerName = manager.first_name + " " + manager.last_name;
                    }
                }

                staffViewModels.Add(new StaffViewModel
                {
                    staff_id = s.staff_id,
                    first_name = s.first_name,
                    last_name = s.last_name,
                    email = s.email,
                    phone = s.phone,
                    active = s.active,
                    store_id = s.store_id,
                    manager_id = s.manager_id,
                    store_name = s.store != null ? s.store.store_name : "N/A",
                    manager_name = managerName
                });
            }

            ViewBag.Staffs = staffViewModels;

            var customerList = await db.customers.ToListAsync();
            ViewBag.Customers = customerList.Select(c => new CustomerViewModel
            {
                customer_id = c.customer_id,
                first_name = c.first_name,
                last_name = c.last_name,
                email = c.email,
                phone = c.phone,
                street = c.street,
                city = c.city,
                state = c.state,
                zip_code = c.zip_code
            }).ToList();

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> CreateStaff(staff newStaff)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    db.staffs.Add(newStaff);
                    await db.SaveChangesAsync();
                    return Json(new { success = true, message = "Staff created successfully!" });
                }
                return Json(new { success = false, message = "Invalid data provided." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult> CreateCustomer(customer newCustomer)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    db.customers.Add(newCustomer);
                    await db.SaveChangesAsync();
                    return Json(new { success = true, message = "Customer created successfully!" });
                }
                return Json(new { success = false, message = "Invalid data provided." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        public async Task<JsonResult> GetStores()
        {
            var stores = await db.stores.Select(s => new { s.store_id, s.store_name }).ToListAsync();
            return Json(stores, JsonRequestBehavior.AllowGet);
        }

        public async Task<JsonResult> GetManagers()
        {
            var managers = await db.staffs
                .Where(s => s.active == 1)
                .Select(s => new { s.staff_id, name = s.first_name + " " + s.last_name })
                .ToListAsync();
            return Json(managers, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}