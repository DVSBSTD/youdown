using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace YouDown
{
    public struct Video
    {
        public string Id { get; set; }
        public string Secret1 { get; set; }
        public string Secret2 { get; set; }
        public string Title { get; set; }
    }

    public class Target
    {
        public static Video Fetch(string source)
        {
            return Catch(Convert(source));
        }

        internal static Video Catch(string url)
        {
            string buffer = GetContent(url);
            if (buffer.IndexOf("Error:") < 0 && buffer.IndexOf("/watch_fullscreen?") > 0)
            {
                int start = 0, end = 0;
                string startTag = "var swfArgs";
                string endTag = "};";
                start = buffer.IndexOf(startTag, StringComparison.CurrentCultureIgnoreCase);
                end = buffer.IndexOf(endTag, start, StringComparison.CurrentCultureIgnoreCase);
                string str = buffer.Substring(start, end - start).Replace(" ", "");

                string vid = str.Substring(str.IndexOf("video_id\":\"") + 11, str.IndexOf("\",", str.IndexOf("video_id")) - str.IndexOf("video_id") - 11);
                string l = str.Substring(str.IndexOf("\"l\":") + 4, str.IndexOf(",", str.IndexOf("\"l\":") + 1) - str.IndexOf("\"l\":") - 4);
                string t = str.Substring(str.IndexOf("t\":\"") + 4, str.IndexOf("\",", str.IndexOf("t\":\"") + 2) - str.IndexOf("t\":\"") - 4);

                int tstart = 0, tend = 0;
                tstart = buffer.IndexOf("<title>") + 17;
                tend = buffer.IndexOf("</title>");
                string title = buffer.Substring(tstart, tend - tstart);
                return new Video { Id = vid, Secret1 = l, Secret2 = t, Title = title };
            }
            else
            {
                return new Video();
            }
        }

        internal static string GetContent(string url)
        {
            string buffer;
            try
            {
                string outputBuffer = "where=46038";
                var req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "POST";
                req.ContentLength = outputBuffer.Length;
                req.ContentType = "application/x-www-form-urlencoded";
                var swOut = new StreamWriter(req.GetRequestStream());
                swOut.Write(outputBuffer);
                swOut.Close();
                var resp = (HttpWebResponse)req.GetResponse();
                var sr = new StreamReader(resp.GetResponseStream());
                buffer = sr.ReadToEnd();
                sr.Close();
            }
            catch (Exception exp)
            {
                buffer = "Error: " + exp.Message.ToString();
            }

            return (buffer);
        }

        internal static string Convert(string url)
        {
            url = url.Replace("www.youtube.com", "youtube.com");
            if (url.IndexOf("http://youtube.com/v/") >= 0)
            {
                url.Replace("http://youtube.com/v/", "http://youtube.com/watch?v=");
            }
            if (url.IndexOf("http://youtube.com/watch?v=") < 0)
            {
                url = "";
            }
            return (url);
        }
    }
}