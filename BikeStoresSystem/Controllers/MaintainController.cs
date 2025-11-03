using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using BikeStoresSystem.ViewModels;

namespace BikeStoresSystem.Controllers
{
    public class MaintainController : Controller
    {
        private BikeStoresEntities db = new BikeStoresEntities();

        public async Task<ActionResult> Index()
        {
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

            var productList = await db.products.Include(p => p.brand).Include(p => p.category).ToListAsync();
            ViewBag.Products = productList.Select(p => new ProductViewModel
            {
                product_id = p.product_id,
                product_name = p.product_name,
                brand_id = p.brand_id,
                category_id = p.category_id,
                model_year = p.model_year,
                list_price = p.list_price,
                brand_name = p.brand.brand_name,
                category_name = p.category.category_name
            }).ToList();

            ViewBag.Brands = await db.brands.ToListAsync();
            ViewBag.Categories = await db.categories.ToListAsync();
            ViewBag.Stores = await db.stores.ToListAsync();

            return View();
        }

        public async Task<JsonResult> GetStaff(int id)
        {
            var staff = await db.staffs.FindAsync(id);
            if (staff == null) return Json(null, JsonRequestBehavior.AllowGet);

            var result = new
            {
                staff.staff_id,
                staff.first_name,
                staff.last_name,
                staff.email,
                staff.phone,
                staff.active,
                staff.store_id,
                staff.manager_id
            };
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public async Task<ActionResult> UpdateStaff(staff updatedStaff)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    db.Entry(updatedStaff).State = EntityState.Modified;
                    await db.SaveChangesAsync();
                    return Json(new { success = true, message = "Staff updated successfully!" });
                }
                return Json(new { success = false, message = "Invalid data provided." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult> DeleteStaff(int id)
        {
            try
            {
                var staff = await db.staffs.FindAsync(id);
                if (staff != null)
                {
                    db.staffs.Remove(staff);
                    await db.SaveChangesAsync();
                    return Json(new { success = true, message = "Staff deleted successfully!" });
                }
                return Json(new { success = false, message = "Staff not found." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        public async Task<JsonResult> GetCustomer(int id)
        {
            var customer = await db.customers.FindAsync(id);
            if (customer == null) return Json(null, JsonRequestBehavior.AllowGet);

            var result = new
            {
                customer.customer_id,
                customer.first_name,
                customer.last_name,
                customer.email,
                customer.phone,
                customer.street,
                customer.city,
                customer.state,
                customer.zip_code
            };
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public async Task<ActionResult> UpdateCustomer(customer updatedCustomer)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    db.Entry(updatedCustomer).State = EntityState.Modified;
                    await db.SaveChangesAsync();
                    return Json(new { success = true, message = "Customer updated successfully!" });
                }
                return Json(new { success = false, message = "Invalid data provided." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult> DeleteCustomer(int id)
        {
            try
            {
                var customer = await db.customers.FindAsync(id);
                if (customer != null)
                {
                    db.customers.Remove(customer);
                    await db.SaveChangesAsync();
                    return Json(new { success = true, message = "Customer deleted successfully!" });
                }
                return Json(new { success = false, message = "Customer not found." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        public async Task<JsonResult> GetProduct(int id)
        {
            var product = await db.products.FindAsync(id);
            if (product == null) return Json(null, JsonRequestBehavior.AllowGet);

            var result = new
            {
                product.product_id,
                product.product_name,
                product.brand_id,
                product.category_id,
                product.model_year,
                product.list_price
            };
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public async Task<ActionResult> UpdateProduct(product updatedProduct)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    db.Entry(updatedProduct).State = EntityState.Modified;
                    await db.SaveChangesAsync();
                    return Json(new { success = true, message = "Product updated successfully!" });
                }
                return Json(new { success = false, message = "Invalid data provided." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult> DeleteProduct(int id)
        {
            try
            {
                var product = await db.products.FindAsync(id);
                if (product != null)
                {
                    db.products.Remove(product);
                    await db.SaveChangesAsync();
                    return Json(new { success = true, message = "Product deleted successfully!" });
                }
                return Json(new { success = false, message = "Product not found." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
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