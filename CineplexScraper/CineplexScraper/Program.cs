using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Data.SqlClient;
using CineplexScraper;
using System.Data;
namespace cineplexScraper
{
    class Program
    {
        static Cinema_ShowTimesEntities data_ctx = new Cinema_ShowTimesEntities();


        static ScheduleData movieCheck(string movieName) 
        {
            var movieDetail = from movie in moviesList
                              where movie.movieName == movieName.Replace(" 3D","")
                              select movie;// moviesList.GroupBy(u => u.movieName).Select(grp => grp.First()).ToList();
            if (movieDetail!=null && movieDetail.Any())
            {
                return movieDetail.First();
            }
            //int i = 0;
            //for (; i < moviesList.Count; i++)
            //{
            //    if (moviesList[i].movieName == movieName || (moviesList[i].movieName.Substring(moviesList[i].movieName.Length - 2, 2) == "3D" && moviesList[i].movieName.Remove(moviesList[i].movieName.Length - 3, 3) == movieName))
            //        return moviesList[i];
            //}
            return null;
        }
        static void cinepaxScraper(ChromeDriver myBrowser)
        {
            myBrowser.Navigate().GoToUrl("http://www.cinepax.com/schedule//0/4");
            while (!myBrowser.FindElementByClassName("owl-wrapper-outer").Displayed)
                Thread.Sleep(3000);

            Thread.Sleep(4000);
            var dateContainers = myBrowser.FindElementByClassName("owl-wrapper-outer").FindElements(By.ClassName("owl-item"));
            foreach (var date in dateContainers)
            {
                if (!date.FindElement(By.TagName("a")).Displayed)
                    myBrowser.FindElementByClassName("owl-next").Click();
                date.FindElement(By.TagName("a")).Click();
                
                while (!myBrowser.FindElementsById("secMovieInfo")[0].Displayed) // spining for loading the first movie record
                    Thread.Sleep(3000);
                Thread.Sleep(4000);

                var movieInfoContainer = myBrowser.FindElementsByCssSelector("section#secMovieInfo.cp-schedule-block");
                IJavaScriptExecutor js = myBrowser as IJavaScriptExecutor;
                js.ExecuteScript("$( '.cp-movieinfo' ).each(function( index ) {$(('a'),index).attr('target','_blank');});");
                foreach (var movieInfo in movieInfoContainer)
                {
                    ScheduleData moviecheck = movieCheck(movieInfo.FindElement(By.TagName("h3")).Text);

                    if (moviecheck == null)
                    {
                        ScheduleData scheduleData = new ScheduleData();
                        movieInfo.FindElement(By.ClassName("cp-movieinfo")).FindElement(By.TagName("a")).Click();
                        Thread.Sleep(2000);
                        myBrowser.SwitchTo().Window(myBrowser.WindowHandles[1]); // SWITCH TO MOVIE WINDOW TO GET DATA

                        var newtabData = myBrowser.FindElementsByTagName("p");
                        scheduleData.description = newtabData[newtabData.Count - 1].Text;
                        foreach (var data in newtabData)
                        {
                            if (data.Text.Contains("Release Date"))
                                scheduleData.ReleaseDate = data.Text.Replace("Release Date:\r\n", "");
                            else if (data.Text.Contains("Genre"))
                                scheduleData.Genre = data.Text.Replace("Genre:\r\n", "");
                            else if (data.Text.Contains("Running Time"))
                                scheduleData.runningTime = data.Text.Replace("Running Time:\r\n", "");
                            else if (data.Text.Contains("Starring"))
                                scheduleData.cast = data.Text.Replace("Starring:\r\n", "");
                            else if (data.Text.Contains("Director"))
                                scheduleData.directorName = data.Text.Replace("Director:\r\n", "");
                        }
                        scheduleData.trailerLink = myBrowser.FindElementByTagName("iframe").GetAttribute("src");

                        Thread.Sleep(5000);
                        myBrowser.SwitchTo().Window(myBrowser.WindowHandles[1]).Close();

                        myBrowser.SwitchTo().Window(myBrowser.WindowHandles[0]);

                        var showContainer = movieInfo.FindElements(By.ClassName("cp-schedule-cinema"));
                        foreach (var show in showContainer)
                        {

                            var screenContainer = show.FindElements(By.TagName("li"));
                            scheduleData.screen = screenContainer[0].Text; //screen
                            for (int i = 1; i < screenContainer.Count; i++)
                            {
                                scheduleData.movieName = movieInfo.FindElement(By.TagName("h3")).Text; //Name of the movie

                                if (scheduleData.movieName.Substring(scheduleData.movieName.Length - 2, 2) == "3D")
                                {
                                    scheduleData._2d_3d = "3D";
                                    scheduleData.movieName = scheduleData.movieName.Remove(scheduleData.movieName.Length - 3, 3);
                                }
                                else
                                    scheduleData._2d_3d = "2D";
                                scheduleData.date = date.FindElement(By.TagName("a")).GetAttribute("onclick").ToString().Substring(19, 10);
                                scheduleData.cenima = show.FindElement(By.TagName("h4")).Text;
                                scheduleData.imgLink = movieInfo.FindElement(By.TagName("img")).GetAttribute("src");
                                scheduleData.timing = screenContainer[i].FindElement(By.TagName("a")).Text.Replace("\r\n", "");

                                print(scheduleData);
                                moviesList.Add(scheduleData);

                            }

                        }


                    }
                    else 
                    {
                        
                      
                        var showContainer = movieInfo.FindElements(By.ClassName("cp-schedule-cinema"));
                        foreach (var show in showContainer)
                        {

                            var screenContainer = show.FindElements(By.TagName("li"));
                            moviecheck.screen = screenContainer[0].Text; //screen
                            for (int i = 1; i < screenContainer.Count; i++)
                            {
                                moviecheck.date = date.FindElement(By.TagName("a")).GetAttribute("onclick").ToString().Substring(19, 10);
                                moviecheck.cenima = show.FindElement(By.TagName("h4")).Text;
                                moviecheck.timing = screenContainer[i].FindElement(By.TagName("a")).Text.Replace("\r\n", "");

                                print(moviecheck);


                            }

                        }


                    }
                }

            }



        }
        static SqlConnection conn = new SqlConnection(@"Server=691ac767-73fd-41bc-99de-a65f007b6261.sqlserver.sequelizer.com;Database=db691ac76773fd41bc99dea65f007b6261;User ID=lkagkpksojppoyei;Password=8RncMgsmVu6eu2VCvRsSGQJPgNpcK6YykEYnvRbFD8YJcyLibYVLAqEqgE48YpHY;");

        static void print(ScheduleData sc)
        {
           
            Console.WriteLine("\n\n"+"Date :"+sc.date);
            Console.WriteLine("Cenima :" + sc.cenima);
            Console.WriteLine("Img Link :" + sc.imgLink);
            Console.WriteLine("Movie Name :" + sc.movieName);
            Console.WriteLine("Description :" + sc.description);
            Console.WriteLine("Cast :" + sc.cast);
            Console.WriteLine("Director Name :" + sc.directorName);
            Console.WriteLine("Trailor Link :" + sc.trailerLink);
            Console.WriteLine("Rating :" + sc.rating);
            Console.WriteLine("Type :" + sc._2d_3d);
            Console.WriteLine("Screen :" + sc.screen);
            Console.WriteLine("Show Timing :" + sc.timing);
            Console.WriteLine("Running Time :" + sc.runningTime);
            Console.WriteLine("Genre :" + sc.Genre);
            Console.WriteLine("Release Date :" + sc.ReleaseDate);


            string sql = "insert into [dbo].[ScheduleData] (date,cenima,imgLink,movieName,description,cast,directorName,trailerLink,rating,_2d_3d,screen,timing,runningTime,Genre,ReleaseDate) values (@date,@cenima,@imgLink,@movieName,@description,@cast,@directorName,@trailerLink,@rating,@_2d_3d ,@screen,@timing ,@runningTime ,@Genre ,@ReleaseDate)";

            SqlCommand cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@date", String.IsNullOrEmpty(sc.date) ? "" : sc.date);
            cmd.Parameters.AddWithValue("@cenima", String.IsNullOrEmpty(sc.cenima) ? "" : sc.cenima);
            cmd.Parameters.AddWithValue("@imgLink", String.IsNullOrEmpty(sc.imgLink) ? "" : sc.imgLink);
            cmd.Parameters.AddWithValue("@movieName", String.IsNullOrEmpty(sc.movieName) ? "" : sc.movieName);
            cmd.Parameters.AddWithValue("@description", String.IsNullOrEmpty(sc.description) ? "" : sc.description);
            cmd.Parameters.AddWithValue("@cast", String.IsNullOrEmpty(sc.cast) ? "" : sc.cast);
            cmd.Parameters.AddWithValue("@directorName", String.IsNullOrEmpty(sc.directorName) ? "" : sc.directorName);
            cmd.Parameters.AddWithValue("@trailerLink", String.IsNullOrEmpty(sc.trailerLink) ? "" : sc.trailerLink);
            cmd.Parameters.AddWithValue("@rating", String.IsNullOrEmpty(sc.rating) ? "" : sc.rating);
            cmd.Parameters.AddWithValue("@_2d_3d", String.IsNullOrEmpty(sc._2d_3d) ? "" : sc._2d_3d);
            cmd.Parameters.AddWithValue("@screen", String.IsNullOrEmpty(sc.screen) ? "" : sc.screen);
            cmd.Parameters.AddWithValue("@timing", String.IsNullOrEmpty(sc.timing) ? "" : sc.timing);
            cmd.Parameters.AddWithValue("@runningTime", String.IsNullOrEmpty(sc.runningTime) ? "" : sc.runningTime);
            cmd.Parameters.AddWithValue("@Genre", String.IsNullOrEmpty(sc.Genre) ? "" : sc.Genre);
            cmd.Parameters.AddWithValue("@ReleaseDate", String.IsNullOrEmpty(sc.ReleaseDate) ? "" : sc.ReleaseDate);
            cmd.ExecuteNonQuery();
        
        }


        static void cinestarScraper(ChromeDriver myBrowser)
        {
            myBrowser.Navigate().GoToUrl("http://www.cinestar.com.pk");
            while (!myBrowser.FindElementByClassName("dayswrapper").Displayed)
                Thread.Sleep(3000);

            var dateContainer = myBrowser.FindElementByClassName("dayswrapper").FindElements(By.TagName("a"));
            foreach (var date in dateContainer)
            {
                date.Click();
                var cinemaContainer = myBrowser.FindElementByClassName("tabwarpper").FindElements(By.TagName("a"));
                foreach (var cinema in cinemaContainer)
                {
                    if (cinema.GetAttribute("name") != "Coming Soon")
                    {
                        cinema.Click();
                        Thread.Sleep(2000);
                        tbl_Cinema cinemaobj = new tbl_Cinema() { Cinema_Name = cinema.GetAttribute("name")};
                        data_ctx.tbl_Cinema.Add(cinemaobj);

                        if (!myBrowser.FindElementById("renderhtmloftabs").Text.Contains("Sorry, there is no movie found in current selection."))
                        {
                            var movie = myBrowser.FindElementById("renderhtmloftabs").FindElements(By.ClassName("movie"));

                            foreach (var moviedetail in movie)
                            {
                                ScheduleData scheduleData = new ScheduleData();
                                var details = moviedetail.FindElement(By.ClassName("details"));
                                scheduleData.movieName = details.FindElement(By.ClassName("title")).Text;// movie title
                                scheduleData.imgLink = moviedetail.FindElement(By.ClassName("movie-img")).FindElement(By.TagName("img")).GetAttribute("src");
                                scheduleData.description = details.FindElement(By.ClassName("description")).Text;// movie description
                                

                                var scheduleContainer = moviedetail.FindElement(By.ClassName("schedule")).FindElements(By.ClassName("time"));
                                foreach (var schedule in scheduleContainer)
                                {
                                    scheduleData.date= date.GetAttribute("name"); //date
                                    scheduleData.cenima= cinema.GetAttribute("name"); //cinema  
                                    scheduleData.rating= details.FindElement(By.ClassName("imdbrate")).Text; //imdb rating 
                                    string type = "";// 2d/3d
                                    if (details.FindElements(By.TagName("div"))[3].GetAttribute("class").Contains("2d"))
                                        type = "2D";
                                    else
                                        type = "3D";
                                    scheduleData._2d_3d= type; // 3d/2d

                                    scheduleData.timing= schedule.Text; // show time 
                                    print(scheduleData);

                                }

                            }
                        }

                    }



                }



            }
        }

        static List<ScheduleData> moviesList = new List<ScheduleData>();
 
        static void Main(string[] args)
        {

            var myBrowser = new ChromeDriver();
            conn.Open();
            
            
            try
            {
                cinepaxScraper(myBrowser);
                cinestarScraper(myBrowser);
             
            }
            catch (Exception e)
            {
                throw e; 
            }
            conn.Close();
            myBrowser.Quit();

            Console.WriteLine("\n\n Movie " );
            foreach (var i in moviesList)
            {
                Console.WriteLine("Movie Name:"+i.movieName);
            }


        }


      
    }

}
