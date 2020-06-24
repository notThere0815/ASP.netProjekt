﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using WebProject;
using WebProject.Models;

namespace WebProject.Controllers
{
    [Authorize]
    public class DashCustomerController : Controller
    {
        private Northwind db = new Northwind();

        public ActionResult Checkout()
        {
            if (Session["cid"] == null) return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
            if (Session["orderid"] == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var order = db.Orders.Find(Session["orderid"]);
            order.Status = 1;
            return RedirectToAction("Index");
        }

        public ActionResult Cart()
        {
            if (Session["cid"] == null) return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
            if (Session["orderid"] == null)
            {
                return View(new List<Order_Detail>());
            }
            int orderid = (int)Session["orderid"];
            var orders = db.Order_Details.Where(t => t.OrderID == orderid);
            return View(orders.ToList());
        }

        public ActionResult OrderProduct(int product)
        {
            if (Session["cid"] == null) return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
            Order order = null;
            if (Session["orderid"] == null)
            {
                // get new order if there is no available
                order = new Order();
                order.Status = -1;
                order.CustomerID = (string)Session["cid"];
                db.Orders.Add(order);
                db.SaveChanges();
                string customerid = (string)Session["cid"];
                order = db.Orders.Where(t => t.CustomerID == customerid && t.Status == -1).FirstOrDefault();
                order.Status = 0;
                Session["orderid"] = order.OrderID;
            }
            var prod = db.Products.Find(product);
            Session["pid"] = prod.ProductID;
            ViewBag.Product = prod.ProductName;
            ViewBag.UnitPrice = prod.UnitPrice;
            db.SaveChanges();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult OrderProduct([Bind(Include = "OrderID,ProductID,Quantity")]Order_Detail detail)
        {
            if (Session["orderid"] == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            if (Session["cid"] == null) return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
            detail.OrderID = (int)Session["orderid"];
            detail.ProductID = (int)Session["pid"];
            db.Order_Details.Add(detail);
            db.SaveChanges();
            return RedirectToAction("Cart");
        }

        public ActionResult Logoff()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginGeneric login, string returnUrl)
        {
            Customer customer;
            if (ModelState.IsValid)
            {
                using (var db = new Northwind())
                {
                    var erg = from t in db.Customers
                              where t.Username == login.Username && t.Password == login.Password
                              select t;
                    customer = erg.FirstOrDefault();
                }
                if (customer != null)
                {
                    FormsAuthentication.SetAuthCookie(login.Username, login.RememberMe);
                    Session["cid"] = customer.CustomerID;
                    return RedirectToAction("Index", "DashCustomer", new { });
                    //return RedirectToLocal(returnUrl);
                }
                else
                {
                    ModelState.AddModelError("", "Invalid username or password");
                }
            }
            return View();
        }

        // GET: DashCustomer
        public ActionResult Index()
        {
            string customer = (string)Session["cid"];
            var orders = db.Orders.Include(o => o.Customer).Include(o => o.Employee).Include(o => o.Shipper);
            orders = orders.Where(t => t.CustomerID == customer);
            return View(orders.ToList());
        }

        // GET: DashCustomer/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Order order = db.Orders.Find(id);
            if (order == null)
            {
                return HttpNotFound();
            }
            return View(order);
        }

        [AllowAnonymous]
        // GET: DashCustomer/Create
        public ActionResult Create()
        {
            ViewBag.CustomerID = new SelectList(db.Customers, "CustomerID", "CompanyName");
            ViewBag.EmployeeID = new SelectList(db.Employees, "EmployeeID", "LastName");
            ViewBag.ShipVia = new SelectList(db.Shippers, "ShipperID", "CompanyName");
            return View();
        }

        // POST: DashCustomer/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [AllowAnonymous]    
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "OrderID,CustomerID,EmployeeID,OrderDate,RequiredDate,ShippedDate,ShipVia,Freight,ShipName,ShipAddress,ShipCity,ShipRegion,ShipPostalCode,ShipCountry")] Order order)
        {
            if (ModelState.IsValid)
            {
                db.Orders.Add(order);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.CustomerID = new SelectList(db.Customers, "CustomerID", "CompanyName", order.CustomerID);
            ViewBag.EmployeeID = new SelectList(db.Employees, "EmployeeID", "LastName", order.EmployeeID);
            ViewBag.ShipVia = new SelectList(db.Shippers, "ShipperID", "CompanyName", order.ShipVia);
            return View(order);
        }

        // GET: DashCustomer/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Order order = db.Orders.Find(id);
            if (order == null)
            {
                return HttpNotFound();
            }
            ViewBag.CustomerID = new SelectList(db.Customers, "CustomerID", "CompanyName", order.CustomerID);
            ViewBag.EmployeeID = new SelectList(db.Employees, "EmployeeID", "LastName", order.EmployeeID);
            ViewBag.ShipVia = new SelectList(db.Shippers, "ShipperID", "CompanyName", order.ShipVia);
            return View(order);
        }

        // POST: DashCustomer/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "OrderID,CustomerID,EmployeeID,OrderDate,RequiredDate,ShippedDate,ShipVia,Freight,ShipName,ShipAddress,ShipCity,ShipRegion,ShipPostalCode,ShipCountry")] Order order)
        {
            if (ModelState.IsValid)
            {
                db.Entry(order).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.CustomerID = new SelectList(db.Customers, "CustomerID", "CompanyName", order.CustomerID);
            ViewBag.EmployeeID = new SelectList(db.Employees, "EmployeeID", "LastName", order.EmployeeID);
            ViewBag.ShipVia = new SelectList(db.Shippers, "ShipperID", "CompanyName", order.ShipVia);
            return View(order);
        }

        // GET: DashCustomer/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Order order = db.Orders.Find(id);
            if (order == null)
            {
                return HttpNotFound();
            }
            return View(order);
        }

        // POST: DashCustomer/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Order order = db.Orders.Find(id);
            db.Orders.Remove(order);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Login", "DashCustomer");
            }
        }
    }
}
