using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace Yggdrasil.Serialization
{
    public static class YggParser
    {
        public static XmlDocument LoadFromFile(string path)
        {
            var document = new XmlDocument();
            var text = new string(ConvertToXml(path).ToArray());

            document.LoadXml(text);

            return document;
        }

        private static IEnumerable<char> ConvertToXml(string path)
        {
	        const char escape = '|';

	        using (var stream = new StreamReader(path))
	        {
		        var openingCdata = "<![CDATA[".ToCharArray();
		        var closeingCdata = "]]>".ToCharArray();
		        var prev = '\0';
		        var started = false;
		        var ampersand = "&amp;".ToCharArray();
		        var lessThan = "&lt;".ToCharArray();
		        var greaterThan = "&gt;".ToCharArray();
		        var quotes = "&quot;".ToCharArray();

		        while (!stream.EndOfStream)
		        {
			        var next = (char)stream.Read();

			        if (prev == '"' && next == escape)
			        {
				        yield return prev;

				        var n0 = '\0';
				        var started0 = false;

				        while (!stream.EndOfStream)
				        {
					        var n = (char)stream.Read();

					        if (n0 == escape && n == '"') { yield return n; break; }

					        if (n == '&')
					        {
						        if (n0 != '&') { yield return n0; }
						        foreach (var c in ampersand) { yield return c; }
						        started0 = false;
						        n0 = n;
					        }
					        else if (n == '"')
					        {
						        if (n0 != '"') { yield return n0; }
						        foreach (var c in quotes) { yield return c; }
						        started0 = false;
						        n0 = n;
					        }
					        else if (n == '<')
					        {
						        if (n0 != '<') { yield return n0; }
						        foreach (var c in lessThan) { yield return c; }
						        started0 = false;
						        n0 = n;
					        }
					        else if (n == '>')
					        {
						        if (n0 != '>') { yield return n0; }
						        foreach (var c in greaterThan) { yield return c; }
						        started0 = false;
						        n0 = n;
					        }
					        else if (started0)
					        {
						        yield return n0;
						        n0 = n;
					        }
					        else
					        {
						        n0 = n;
						        started0 = true;
					        }
				        }

				        started = false;
			        }
			        else if (prev == '>' && next == escape)
			        {
				        yield return prev;

				        foreach (var c in openingCdata) { yield return c; }

				        started = false;
			        }
			        else if (prev == escape && next == '<')
			        {
				        foreach (var c in closeingCdata) { yield return c; }

				        started = false;
				        yield return next;
			        }
			        else if (started) { yield return prev; prev = next; }
			        else { prev = next; started = true; }
		        }
	        }
        }
    }
}
