using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using BikeStoresSystem;
using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace BikeStoresSystem.Controllers
{
    public class ReportController : Controller
    {
        private BikeStoresEntities db = new BikeStoresEntities();

        public ActionResult Index()
        {
            return View();
        }

        public async Task<JsonResult> GetSalesReportData()
        {
            var salesData = await db.order_items
                .Include(oi => oi.order)
                .Include(oi => oi.product)
                .Include(oi => oi.order.customer)
                .Include(oi => oi.order.staff)
                .Select(oi => new
                {
                    OrderId = oi.order_id,
                    OrderDate = oi.order.order_date,
                    CustomerName = oi.order.customer.first_name + " " + oi.order.customer.last_name,
                    ProductName = oi.product.product_name,
                    StaffName = oi.order.staff.first_name + " " + oi.order.staff.last_name,
                    Quantity = oi.quantity,
                    Price = oi.list_price,
                    Discount = oi.discount,
                    Total = (oi.quantity * oi.list_price) * (1 - oi.discount)
                })
                .OrderByDescending(x => x.OrderDate)
                .ToListAsync();

            return Json(salesData, JsonRequestBehavior.AllowGet);
        }

        public async Task<JsonResult> GetPopularProductsData()
        {
            var popularProducts = await db.order_items
                .Include(oi => oi.product)
                .Include(oi => oi.product.brand)
                .Include(oi => oi.product.category)
                .GroupBy(oi => new
                {
                    oi.product.product_id,
                    oi.product.product_name,
                    BrandName = oi.product.brand.brand_name,
                    CategoryName = oi.product.category.category_name
                })
                .Select(g => new
                {
                    ProductId = g.Key.product_id,
                    ProductName = g.Key.product_name,
                    Brand = g.Key.BrandName,
                    Category = g.Key.CategoryName,
                    TotalOrders = g.Count(),
                    TotalQuantity = g.Sum(oi => oi.quantity)
                })
                .OrderByDescending(x => x.TotalQuantity)
                .Take(10)
                .ToListAsync();

            return Json(popularProducts, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public async Task<ActionResult> SaveReport(string fileName, string fileType, string reportType, string description)
        {
            try
            {
                string reportsPath = Server.MapPath("~/Reports");
                if (!Directory.Exists(reportsPath))
                {
                    Directory.CreateDirectory(reportsPath);
                }

                string fullFileName = $"{fileName}_{DateTime.Now:yyyyMMddHHmmss}.{fileType}";
                string filePath = Path.Combine(reportsPath, fullFileName);

                if (reportType == "sales")
                {
                    var salesData = await db.order_items
                        .Include(oi => oi.order)
                        .Include(oi => oi.product)
                        .Include(oi => oi.order.customer)
                        .Include(oi => oi.order.staff)
                        .Select(oi => new
                        {
                            OrderId = oi.order_id,
                            OrderDate = oi.order.order_date,
                            CustomerName = oi.order.customer.first_name + " " + oi.order.customer.last_name,
                            ProductName = oi.product.product_name,
                            StaffName = oi.order.staff.first_name + " " + oi.order.staff.last_name,
                            Quantity = oi.quantity,
                            Price = oi.list_price,
                            Discount = oi.discount,
                            Total = (oi.quantity * oi.list_price) * (1 - oi.discount)
                        })
                        .OrderByDescending(x => x.OrderDate)
                        .ToListAsync();

                    if (fileType.ToLower() == "xlsx")
                    {
                        GenerateExcelReport(salesData, filePath, "Current Sales Report");
                    }
                    else if (fileType.ToLower() == "pdf")
                    {
                        GeneratePdfReport(salesData, filePath, "Current Sales Report");
                    }
                }
                else if (reportType == "popular")
                {
                    var popularProducts = await db.order_items
                        .Include(oi => oi.product)
                        .Include(oi => oi.product.brand)
                        .Include(oi => oi.product.category)
                        .GroupBy(oi => new
                        {
                            oi.product.product_id,
                            oi.product.product_name,
                            BrandName = oi.product.brand.brand_name,
                            CategoryName = oi.product.category.category_name
                        })
                        .Select(g => new
                        {
                            ProductId = g.Key.product_id,
                            ProductName = g.Key.product_name,
                            Brand = g.Key.BrandName,
                            Category = g.Key.CategoryName,
                            TotalOrders = g.Count(),
                            TotalQuantity = g.Sum(oi => oi.quantity)
                        })
                        .OrderByDescending(x => x.TotalQuantity)
                        .ToListAsync();

                    if (fileType.ToLower() == "xlsx")
                    {
                        GenerateExcelReportPopular(popularProducts, filePath, "Popular Products Report");
                    }
                    else if (fileType.ToLower() == "pdf")
                    {
                        GeneratePdfReportPopular(popularProducts, filePath, "Popular Products Report");
                    }
                }

                if (!string.IsNullOrEmpty(description))
                {
                    string descFileName = Path.GetFileNameWithoutExtension(fullFileName) + "_description.txt";
                    string descFilePath = Path.Combine(reportsPath, descFileName);
                    System.IO.File.WriteAllText(descFilePath, description);
                }

                return Json(new { success = true, message = "Report saved successfully!", fileName = fullFileName });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        private void GenerateExcelReport(dynamic data, string filePath, string title)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add(title);

                worksheet.Cell(1, 1).Value = "Order ID";
                worksheet.Cell(1, 2).Value = "Order Date";
                worksheet.Cell(1, 3).Value = "Customer";
                worksheet.Cell(1, 4).Value = "Product";
                worksheet.Cell(1, 5).Value = "Staff";
                worksheet.Cell(1, 6).Value = "Quantity";
                worksheet.Cell(1, 7).Value = "Price";
                worksheet.Cell(1, 8).Value = "Discount";
                worksheet.Cell(1, 9).Value = "Total";

                var headerRange = worksheet.Range(1, 1, 1, 9);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

                int row = 2;
                foreach (var item in data)
                {
                    worksheet.Cell(row, 1).Value = item.OrderId;
                    worksheet.Cell(row, 2).Value = item.OrderDate?.ToString("yyyy-MM-dd");
                    worksheet.Cell(row, 3).Value = item.CustomerName;
                    worksheet.Cell(row, 4).Value = item.ProductName;
                    worksheet.Cell(row, 5).Value = item.StaffName;
                    worksheet.Cell(row, 6).Value = item.Quantity;
                    worksheet.Cell(row, 7).Value = item.Price;
                    worksheet.Cell(row, 8).Value = item.Discount;
                    worksheet.Cell(row, 9).Value = item.Total;
                    row++;
                }

                worksheet.Columns().AdjustToContents();
                workbook.SaveAs(filePath);
            }
        }

        private void GenerateExcelReportPopular(dynamic data, string filePath, string title)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add(title);

                worksheet.Cell(1, 1).Value = "Product ID";
                worksheet.Cell(1, 2).Value = "Product Name";
                worksheet.Cell(1, 3).Value = "Brand";
                worksheet.Cell(1, 4).Value = "Category";
                worksheet.Cell(1, 5).Value = "Total Orders";
                worksheet.Cell(1, 6).Value = "Total Quantity";

                var headerRange = worksheet.Range(1, 1, 1, 6);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGreen;

                int row = 2;
                foreach (var item in data)
                {
                    worksheet.Cell(row, 1).Value = item.ProductId;
                    worksheet.Cell(row, 2).Value = item.ProductName;
                    worksheet.Cell(row, 3).Value = item.Brand;
                    worksheet.Cell(row, 4).Value = item.Category;
                    worksheet.Cell(row, 5).Value = item.TotalOrders;
                    worksheet.Cell(row, 6).Value = item.TotalQuantity;
                    row++;
                }

                worksheet.Columns().AdjustToContents();
                workbook.SaveAs(filePath);
            }
        }

        private void GeneratePdfReport(dynamic data, string filePath, string title)
        {
            Document document = new Document(PageSize.A4.Rotate());
            PdfWriter.GetInstance(document, new FileStream(filePath, FileMode.Create));
            document.Open();

            Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
            Paragraph titlePara = new Paragraph(title, titleFont);
            titlePara.Alignment = Element.ALIGN_CENTER;
            document.Add(titlePara);
            document.Add(new Paragraph(" "));

            PdfPTable table = new PdfPTable(9);
            table.WidthPercentage = 100;

            string[] headers = { "Order ID", "Date", "Customer", "Product", "Staff", "Qty", "Price", "Disc", "Total" };
            foreach (string header in headers)
            {
                PdfPCell cell = new PdfPCell(new Phrase(header, FontFactory.GetFont(FontFactory.HELVETICA_BOLD)));
                cell.BackgroundColor = BaseColor.LIGHT_GRAY;
                table.AddCell(cell);
            }

            foreach (var item in data)
            {
                table.AddCell(item.OrderId.ToString());
                table.AddCell(item.OrderDate?.ToString("yyyy-MM-dd") ?? "");
                table.AddCell(item.CustomerName ?? "");
                table.AddCell(item.ProductName ?? "");
                table.AddCell(item.StaffName ?? "");
                table.AddCell(item.Quantity.ToString());
                table.AddCell(item.Price.ToString("C"));
                table.AddCell(item.Discount.ToString("P"));
                table.AddCell(item.Total.ToString("C"));
            }

            document.Add(table);
            document.Close();
        }

        private void GeneratePdfReportPopular(dynamic data, string filePath, string title)
        {
            Document document = new Document(PageSize.A4);
            PdfWriter.GetInstance(document, new FileStream(filePath, FileMode.Create));
            document.Open();

            Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
            Paragraph titlePara = new Paragraph(title, titleFont);
            titlePara.Alignment = Element.ALIGN_CENTER;
            document.Add(titlePara);
            document.Add(new Paragraph(" "));

            PdfPTable table = new PdfPTable(6);
            table.WidthPercentage = 100;

            string[] headers = { "Product ID", "Product Name", "Brand", "Category", "Total Orders", "Total Quantity" };
            foreach (string header in headers)
            {
                PdfPCell cell = new PdfPCell(new Phrase(header, FontFactory.GetFont(FontFactory.HELVETICA_BOLD)));
                cell.BackgroundColor = BaseColor.LIGHT_GRAY;
                table.AddCell(cell);
            }

            foreach (var item in data)
            {
                table.AddCell(item.ProductId.ToString());
                table.AddCell(item.ProductName ?? "");
                table.AddCell(item.Brand ?? "");
                table.AddCell(item.Category ?? "");
                table.AddCell(item.TotalOrders.ToString());
                table.AddCell(item.TotalQuantity.ToString());
            }

            document.Add(table);
            document.Close();
        }

        public JsonResult GetArchivedReports()
        {
            string reportsPath = Server.MapPath("~/Reports");
            List<object> reports = new List<object>();

            if (Directory.Exists(reportsPath))
            {
                var files = Directory.GetFiles(reportsPath)
                    .Where(f => !f.EndsWith("_description.txt"))
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(fi => fi.CreationTime)
                    .ToList();

                foreach (var file in files)
                {
                    string descFile = Path.Combine(reportsPath, Path.GetFileNameWithoutExtension(file.Name) + "_description.txt");
                    string description = System.IO.File.Exists(descFile) ? System.IO.File.ReadAllText(descFile) : "";

                    reports.Add(new
                    {
                        fileName = file.Name,
                        fileSize = (file.Length / 1024.0).ToString("0.00") + " KB",
                        createdDate = file.CreationTime.ToString("yyyy-MM-dd HH:mm"),
                        description = description
                    });
                }
            }

            return Json(reports, JsonRequestBehavior.AllowGet);
        }

        public ActionResult DownloadReport(string fileName)
        {
            string filePath = Path.Combine(Server.MapPath("~/Reports"), fileName);
            if (System.IO.File.Exists(filePath))
            {
                byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
                return File(fileBytes, "application/octet-stream", fileName);
            }
            return HttpNotFound();
        }

        [HttpPost]
        public ActionResult DeleteReport(string fileName)
        {
            try
            {
                string filePath = Path.Combine(Server.MapPath("~/Reports"), fileName);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);

                    string descFile = Path.Combine(Server.MapPath("~/Reports"),
                        Path.GetFileNameWithoutExtension(fileName) + "_description.txt");
                    if (System.IO.File.Exists(descFile))
                    {
                        System.IO.File.Delete(descFile);
                    }

                    return Json(new { success = true, message = "Report deleted successfully!" });
                }
                return Json(new { success = false, message = "File not found." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public ActionResult UpdateReportDescription(string fileName, string description)
        {
            try
            {
                string descFileName = Path.GetFileNameWithoutExtension(fileName) + "_description.txt";
                string descFilePath = Path.Combine(Server.MapPath("~/Reports"), descFileName);
                System.IO.File.WriteAllText(descFilePath, description);
                return Json(new { success = true, message = "Description updated successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
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