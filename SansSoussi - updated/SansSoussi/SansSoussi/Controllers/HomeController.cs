﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.SqlClient;
using System.Web.Configuration;
using System.Web.Security;
using Microsoft.Security.Application;

namespace SansSoussi.Controllers
{
    public class HomeController : Controller
    {
        SqlConnection _dbConnection;
        public HomeController()
        {
             _dbConnection = new SqlConnection(WebConfigurationManager.ConnectionStrings["ApplicationServices"].ConnectionString);
        }

        public ActionResult Index()
        {
            ViewBag.Message = "Parce que marcher devrait se faire SansSoussi";

            return View();
        }

        public ActionResult Comments()
        {
            List<string> comments = new List<string>();

            //Get current user from default membership provider
            MembershipUser user = Membership.Provider.GetUser(HttpContext.User.Identity.Name, true);
            if (user != null)
            {
                SqlCommand cmd = new SqlCommand("Select Comment from Comments where UserId ='" + user.ProviderUserKey + "'", _dbConnection);
                _dbConnection.Open();
                SqlDataReader rd = cmd.ExecuteReader();

                while (rd.Read())
                {
                    comments.Add(rd.GetString(0));
                }

                rd.Close();
                _dbConnection.Close();
            }
            return View(comments);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Comments(string comment)
        {
           
            string status = "success";
            try
            {
                //Get current user from default membership provider
                MembershipUser user = Membership.Provider.GetUser(HttpContext.User.Identity.Name, true);
                if (user != null)
                {
                    //Validating comment and removing XSS attempts
                    comment = Sanitizer.GetSafeHtmlFragment(comment);

                    //add new comment to db
                    SqlCommand cmd = new SqlCommand(
                        "insert into Comments (UserId, CommentId, Comment) Values (@UserId, @CommentId, @Comment)", _dbConnection);
                    //('" + user.ProviderUserKey + "','" + Guid.NewGuid() + "','" + comment + "')",

                    cmd.Parameters.AddWithValue("@UserId", user.ProviderUserKey);
                    cmd.Parameters.AddWithValue("@CommentId", Guid.NewGuid());
                    cmd.Parameters.AddWithValue("@Comment", comment);
                    _dbConnection.Open();

                    cmd.ExecuteNonQuery();
                }
                else
                {
                    throw new Exception("Vous devez vous connecter");
                }
            }
            catch (Exception ex)
            {
                status = ex.Message;
            }
            finally
            {
                _dbConnection.Close();
            }

            return Json(status);
        }

        public ActionResult Search(string searchData)
        {
            List<string> searchResults = new List<string>();

            //Get current user from default membership provider
            MembershipUser user = Membership.Provider.GetUser(HttpContext.User.Identity.Name, true);
            if (user != null)
            {
                //Validating comment and removing XSS attempts
                searchData = Sanitizer.GetSafeHtmlFragment(searchData);

                if (!string.IsNullOrEmpty(searchData))
                {
                    SqlCommand cmd = new SqlCommand("Select Comment from Comments where UserId = @0 and Comment LIKE @1", _dbConnection);
                    cmd.Parameters.AddWithValue("@0", user.ProviderUserKey);
                    cmd.Parameters.AddWithValue("@1", '%' + searchData + '%');
                    _dbConnection.Open();
                    SqlDataReader rd = cmd.ExecuteReader();


                    while (rd.Read())
                    {
                        searchResults.Add(rd.GetString(0));
                    }

                    rd.Close();
                    _dbConnection.Close();
                }
            }
            return View(searchResults);
        }

        [HttpGet]
        public ActionResult Emails()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Emails(object form)
        {
            List<string> searchResults = new List<string>();

            HttpCookie cookie = HttpContext.Request.Cookies["username"];
            
            List<string> cookieString = new List<string>();

            //Decode the cookie from base64 encoding
            byte[] encodedDataAsBytes = System.Convert.FromBase64String(cookie.Value);
            string strCookieValue = System.Text.ASCIIEncoding.ASCII.GetString(encodedDataAsBytes);

            //get user role base on cookie value
            string[] roles = Roles.GetRolesForUser(strCookieValue);

            bool isAdmin = roles.Contains("admin");

            if (isAdmin)
            {
                SqlCommand cmd = new SqlCommand("Select Email from aspnet_Membership", _dbConnection);
                _dbConnection.Open();
                SqlDataReader rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    searchResults.Add(rd.GetString(0));
                }
                rd.Close();
                _dbConnection.Close();
            }


            return Json(searchResults);
        }

        public ActionResult About()
        {
            return View();
        }
    }
}
