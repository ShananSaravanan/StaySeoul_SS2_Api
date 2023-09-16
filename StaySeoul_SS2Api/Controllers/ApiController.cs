using Microsoft.Ajax.Utilities;
using StaySeoul_SS2Api.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace StaySeoul_SS2Api.Controllers
{
    public class ApiController : Controller
    {
        SS2Entities ent = new SS2Entities();
        // GET: Api
        public ActionResult Index()
        {
            return View();
        }
        public static string username = "";
        public JsonResult Login(User user)
        {
            var loginUser = ent.Users.Where(x => x.Username == user.Username && x.Password == user.Password).FirstOrDefault();
            if (loginUser != null)
            {
                LoginUser loggedInUser = new LoginUser();
                loggedInUser.Username = user.Username;
                username = loggedInUser.Username;
                return Json(loggedInUser, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(null, JsonRequestBehavior.AllowGet);
            }
        }
        public JsonResult GetPropertyList()
        {
            if (username != null)
            {
                var properties = ent.Items.Where(x => x.User.Username == username).ToList();
                if (properties.Any())
                {

                    List<Property> PropertyList = new List<Property>();
                    List<PropertyCount> CountList = new List<PropertyCount>();
                    CountList.Clear();
                    PropertyList.Clear();
                    foreach (var property in properties)
                    {
                        PropertyCount propertyCount = new PropertyCount();
                        var itemPrice = ent.ItemPrices.Where(x => x.ItemID == property.ID);
                        if (itemPrice.Any())
                        {
                            var booked = ent.BookingDetails.Where(x => x.ItemPrice.ItemID == property.ID);

                            var unbooked = itemPrice.Select(x => x.ID).Except(booked.Select(y => y.ItemPrice.ID));

                            propertyCount.itemID = property.ID;
                            propertyCount.count = unbooked.Count();
                            CountList.Add(propertyCount);
                        }
                        else
                        {
                            propertyCount.itemID = property.ID;
                            propertyCount.count = 0;
                            CountList.Add(propertyCount);
                        }
                    }
                    CountList = CountList.OrderBy(x => x.count).ToList();


                    foreach (var cL in CountList)
                    {
                        foreach (var property in properties)
                        {
                            if (property.ID == cL.itemID)
                            {
                                DateTime sysdate = DateTime.Today;
                                sysdate = sysdate.AddDays(-5);
                                Property p = new Property();
                                p.propertyName = property.Title;
                                var lastDate = ent.ItemPrices.Where(x => x.Item.ID == property.ID).OrderByDescending(x => x.Date).FirstOrDefault();
                                if (lastDate != null)
                                {
                                    p.dateDetail = "Last date of pricing: " + lastDate.Date.ToShortDateString();
                                    if (lastDate.Date < sysdate)
                                    {
                                        p.fontColor = "Red";
                                    }


                                }
                                else
                                {
                                    p.dateDetail = "Last date of pricing:-";
                                    p.lastDate = "";
                                    p.fontColor = "Red";
                                }
                                PropertyList.Add(p);
                            }
                        }
                    }

                    return Json(PropertyList, JsonRequestBehavior.AllowGet); ;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
        public JsonResult GetPriceList(PropertyPrice property)
        {

            List<PropertyPrice> PriceList = new List<PropertyPrice>();
            PriceList.Clear();
            var itemPrices = ent.ItemPrices.Where(x => x.Item.Title == property.title).ToList();
            if (itemPrices.Any())
            {
                foreach (var iP in itemPrices)
                {
                    PropertyPrice p = new PropertyPrice();
                    p.ID = iP.ID;
                    p.title = property.title;
                    p.price = iP.Price;
                    p.rules = iP.CancellationPolicy.Name;
                    p.date = iP.Date;
                    var checkHoliday = ent.DimDates.Where(x => x.Date == iP.Date && x.isHoliday == true).FirstOrDefault();
                    if (checkHoliday != null)
                    {
                        p.iconName = "mug.png";
                    }
                    var booked = ent.BookingDetails.Where(x => x.ItemPriceID == iP.ID).FirstOrDefault();
                    if (booked != null)
                    {
                        p.iconName = "lock.png";
                        p.color = "#BEBBBB";
                    }
                    PriceList.Add(p);
                }
                return Json(PriceList, JsonRequestBehavior.AllowGet);
            }
            return null;
        }
        public JsonResult DeletePrice(PropertyPrice property)
        {
            var itemRemove = ent.ItemPrices.Where(x => x.ID == property.ID).FirstOrDefault();
            if (itemRemove != null)
            {
                ent.ItemPrices.Remove(itemRemove);
                ent.SaveChanges();
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }

        }
        public JsonResult AddPrice(ItemPrice propertyPrice)
        {
            System.DateTime priceDate = propertyPrice.Date.Date;
            var itemPrice = ent.ItemPrices.Where(x => x.Date == priceDate && x.Item.Title == propertyPrice.itemTitle).FirstOrDefault();

            if (itemPrice != null)
            {
                propertyPrice.ItemID = ent.Items.Where(x => x.Title == propertyPrice.itemTitle).Select(x => x.ID).FirstOrDefault();
                ent.ItemPrices.Remove(itemPrice);
                ent.SaveChanges();
            }
            var checkHoliday = ent.DimDates.Where(x => x.Date == propertyPrice.Date && x.isHoliday == true).FirstOrDefault();
            if (checkHoliday != null && propertyPrice.weekendPrice!=0)
            {
                propertyPrice.ID = ent.ItemPrices.Count() + 1;
                propertyPrice.ItemID = ent.Items.Where(x => x.Title == propertyPrice.itemTitle).Select(x => x.ID).FirstOrDefault();
                propertyPrice.Price = propertyPrice.holidayPrice;
                propertyPrice.GUID = Guid.NewGuid();
                propertyPrice.CancellationPolicyID = propertyPrice.holidayCancellationPolicy;
                ent.ItemPrices.Add(propertyPrice);
                ent.SaveChanges();
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            DayOfWeek sunday = DayOfWeek.Sunday;
            DayOfWeek saturday = DayOfWeek.Saturday;
            if (propertyPrice.Date.DayOfWeek == sunday && propertyPrice.weekendPrice!=0 || propertyPrice.Date.DayOfWeek == saturday && propertyPrice.weekendPrice != 0)
            {
                
                    var checkHolidayAgain = ent.DimDates.Where(x => x.Date == propertyPrice.Date && x.isHoliday == true).FirstOrDefault();
                    if (checkHolidayAgain != null && propertyPrice.holidayPrice != 0)
                    {
                        propertyPrice.ID = ent.ItemPrices.Count() + 1;
                        propertyPrice.ItemID = ent.Items.Where(x => x.Title == propertyPrice.itemTitle).Select(x => x.ID).FirstOrDefault();
                        propertyPrice.Price = propertyPrice.holidayPrice;
                        propertyPrice.GUID = Guid.NewGuid();
                        propertyPrice.CancellationPolicyID = propertyPrice.holidayCancellationPolicy;
                        ent.ItemPrices.Add(propertyPrice);
                        ent.SaveChanges();
                        return Json(true, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        propertyPrice.Price = propertyPrice.weekendPrice;
                        propertyPrice.ItemID = ent.Items.Where(x => x.Title == propertyPrice.itemTitle).Select(x => x.ID).FirstOrDefault();
                        propertyPrice.GUID = Guid.NewGuid();
                        propertyPrice.CancellationPolicyID = propertyPrice.weekendCancellationPolicy;
                        ent.ItemPrices.Add(propertyPrice);
                        ent.SaveChanges();
                        return Json(true, JsonRequestBehavior.AllowGet);
                    }
                

            }
            else
            {
                propertyPrice.ID = ent.ItemPrices.Count() + 1;
                propertyPrice.GUID = Guid.NewGuid();
                propertyPrice.ItemID = ent.Items.Where(x => x.Title == propertyPrice.itemTitle).Select(x => x.ID).FirstOrDefault();
                propertyPrice.CancellationPolicyID = propertyPrice.normalCancellationPolicy;
                ent.ItemPrices.Add(propertyPrice);
                ent.SaveChanges();
                return Json(true, JsonRequestBehavior.AllowGet);
            }
        }







    }
    }

