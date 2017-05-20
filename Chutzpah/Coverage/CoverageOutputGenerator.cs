using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using Chutzpah.Models;
using Chutzpah.Wrappers;

namespace Chutzpah.Coverage
{
    public static class CoverageOutputGenerator
    {
        public static string WriteHtmlFile(string path, CoverageData coverage)
        {
            using (var fileStream = new FileStream(path, FileMode.Create))
            {
                GenerateHtml(coverage, fileStream);
            }

            return path;
        }

        public static string WriteJsonFile(string path, CoverageData coverage)
        {
            using (var fileStream = new FileStream(path, FileMode.Create))
            using (var writer = new StreamWriter(fileStream))
            {
                var serializer = new JsonSerializer();
                writer.Write(serializer.Serialize(coverage));
            }


            return path;
        }

        public static void GenerateHtml(CoverageData coverage, Stream stream)
        {
            var successPercentage = coverage.SuccessPercentage.HasValue ? coverage.SuccessPercentage.Value : Constants.DefaultCodeCoverageSuccessPercentage;

            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine(HtmlFragments.BodyContentStartFormat, HtmlFragments.Js, HtmlFragments.Css);

                var totalLines = 0;
                var totalLinesCovered = 0;

                var fileNumber = 0;
                foreach (var pair in coverage)
                {
                    fileNumber++;
                    var fileName = pair.Key;
                    var fileData = pair.Value;
                    var totalSmts = 0;
                    var linesCovered = 0;
                    var markup = new string[fileData.SourceLines.Length + 1];

                    var maxLineNumberLength = fileData.SourceLines.Length.ToString(CultureInfo.InvariantCulture).Length;
                    for (var i = 0; i < fileData.SourceLines.Length; i++)
                    {
                        var lineNumber = (i + 1).ToString(CultureInfo.InvariantCulture).PadLeft(maxLineNumberLength);
                        lineNumber = lineNumber.Replace(" ", "&nbsp;&nbsp;");
                        var line = HttpUtility.HtmlEncode(fileData.SourceLines[i]).Replace(" ", "&nbsp;");
                        markup[i + 1] = "<div class='{{executed}}'><span class=''>" + lineNumber + "</span>" + line +
                                        "</div>";
                    }

                    for (var i = 1; i < fileData.LineExecutionCounts.Length; i++)
                    {
                        var lineExecution = fileData.LineExecutionCounts[i];
                        if (lineExecution.HasValue)
                        {
                            totalSmts++;
                            if (lineExecution > 0)
                            {
                                linesCovered += 1;
                                markup[i] = markup[i].Replace("{{executed}}", "hit");
                            }
                            else
                            {
                                markup[i] = markup[i].Replace("{{executed}}", "miss");
                            }
                        }
                        else
                        {
                            markup[i] = markup[i].Replace("{{executed}}", "");
                        }
                    }

                    totalLinesCovered += linesCovered;
                    totalLines += totalSmts;

                    AppendResultLine(successPercentage, linesCovered, totalSmts, fileName, fileNumber, markup, writer);
                }


                AppendResultLine(successPercentage, totalLinesCovered, totalLines, "Total", 1 + fileNumber, new string[0], writer);
                writer.WriteLine(HtmlFragments.BodyContentEnd);
            }
        }

        private static void AppendResultLine(double successPercentage, int linesCovered, int totalSmts, string fileName, int fileNumber, string[] markup, StreamWriter writer)
        {
            var result = FormatPercentage(linesCovered, totalSmts);

            var output = HtmlFragments.FileTemplate.Replace("{{file}}", fileName)
                .Replace("{{percentage}}", result.ToString(CultureInfo.InvariantCulture))
                .Replace("{{numberCovered}}", linesCovered.ToString(CultureInfo.InvariantCulture))
                .Replace("{{fileNumber}}", fileNumber.ToString(CultureInfo.InvariantCulture))
                .Replace("{{totalSmts}}", totalSmts.ToString(CultureInfo.InvariantCulture))
                .Replace("{{source}}", String.Join(" ", markup));
            if (result < successPercentage)
            {
                output = output.Replace("{{statusclass}}", "chutzpah-error");
            }
            else
            {
                output = output.Replace("{{statusclass}}", "chutzpah-success");
            }

            writer.WriteLine(output);
        }

        public static double FormatPercentage(int number, int total)
        {
            return Math.Round((number / (double)total) * 100, 2);
        }

        private static class HtmlFragments
        {
            public const string Css = @"
#chutzpah-main
{
  background:#EEE;
  clear:both;
  color:#333;
  font-family:'Helvetica Neue Light', HelveticaNeue-Light, 'Helvetica Neue', Calibri, Helvetica, Arial, sans-serif;
  font-size:17px;
  margin:2px;
}

#chutzpah-main a
{
  color:#333;
  text-decoration:none;
}

#chutzpah-main a:hover
{
  text-decoration:underline;
}

.chutzpah
{
  border-bottom:1px solid #FFF;
  clear:both;
  margin:0;
  padding:5px;
}

.chutzpah-error
{
  color:red;
}

.chutzpah-success
{
  color:#5E7D00;
}

.chutzpah-file
{
  width:auto;
}

.chutzpah-cl
{
  float:left;
}

.chutzpah div.rs
{
  float:right;
  margin-left:50px;
  width:150px;
}

.chutzpah-nb
{
  padding-right:10px;
}

#chutzpah-main a.chutzpah-logo
{
  color:#3245FF;
  cursor:pointer;
  font-weight:700;
  text-decoration:none;
}

.chutzpah-source
{
  background-color:#FFF;
  border:1px solid #CBCBCB;
  color:#363636;
  margin:25px 20px;
  overflow-x:scroll;
  width:80%;
}

.chutzpah-source div
{
  white-space:nowrap;
}

.chutzpah-source span
{
  background-color:#EAEAEA;
  color:#949494;
  display:inline-block;
  padding:0 10px;
  text-align:center;
}

.chutzpah-source .miss
{
  background-color:#e6c3c7;
}
";

            public const string Js = @"
function chutzpah_toggleSource(id) {
        var element = document.getElementById(id);
        if(element.style.display === 'block') {
            element.style.display = 'none';
        } else {
            element.style.display = 'block';
        }
    }
";

            public const string BodyContentStartFormat = @"
<html>
<head>
    <title>Chutzpah Code Coverage Results</title>
    <script type='text/javascript'>
        {0}
    </script>
    <style>
        {1}
    </style>
</head>
<body>
<div id='chutzpah-main'>
	<div class='chutzpah chutzpah-title'>
		<div class='chutzpah-cl chutzpah-file'>
			<a href='https://github.com/mmanela/chutzpah' target='_blank' class='chutzpah-logo'>Chutzpah</a> code coverage via <a href='http://blanketjs.org' target='_blank' class='chutzpah-logo'>blanket.js</a> results
		</div>
		<div class='chutzpah-cl rs'>
			Coverage (%)
		</div>
		<div class='chutzpah-cl rs'>
			Covered/Total Smts.
		</div>
		<div style='clear:both;'>
		</div>
	</div>
";

            public const string BodyContentEnd = @"
</div>
</body>
</html>
";

            public const string FileTemplate = @"
<div class='chutzpah {{statusclass}}'>
	<div class='chutzpah-cl chutzpah-file'>
		<span class='chutzpah-nb'>{{fileNumber}}.</span><a href='javascript:chutzpah_toggleSource(""file-{{fileNumber}}"")'>{{file}}</a>
	</div>
	<div class='chutzpah-cl rs'>
		{{percentage}} %
	</div>
	<div class='chutzpah-cl rs'>
		{{numberCovered}}/{{totalSmts}}
	</div>
	<div id='file-{{fileNumber}}' class='chutzpah-source' style='display:none;'>
		{{source}}
	</div>
	<div style='clear:both;'>
	</div>
</div>
";
        }
    }
}
