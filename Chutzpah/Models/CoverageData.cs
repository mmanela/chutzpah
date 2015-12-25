using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Web.UI;

namespace Chutzpah.Models
{
    /// <summary>
    /// Coverage data is a dictionary that maps file paths to coverage data about a
    /// particular file.
    /// </summary>
    public class CoverageData : Dictionary<string, CoverageFileData>
    {

        private double? coveragePercentage;

        public CoverageData()
        {
        }

        public CoverageData(double successPercentage)
        {
            SuccessPercentage = successPercentage; 
        }

        public double? SuccessPercentage { get; set; }

        /// <summary>
        /// The average percentage of line that were covered
        /// </summary>
        public double CoveragePercentage
        {
            get
            {
                if (!this.Any())
                {
                    return 0;
                }

                if (!coveragePercentage.HasValue)
                {
                    var pairs = from file in Values
                                select file.GetCoveredCount();

                    var sumPair = pairs.Aggregate((x, y) => Tuple.Create(x.Item1 + y.Item1, x.Item2 + y.Item2));
                    var percentage = sumPair.Item1 / sumPair.Item2;

                    coveragePercentage = percentage;
                }

                return coveragePercentage.Value;
            }
        }

        /// <summary>
        /// Merges two coverage objects together, mutating the current one
        /// </summary>
        /// <param name="coverageData"></param>
        public void Merge(CoverageData coverageData)
        {
            foreach (var pair in coverageData)
            {
                if (this.ContainsKey(pair.Key))
                {
                    this[pair.Key].Merge(pair.Value);
                }
                else
                {
                    this[pair.Key] = new CoverageFileData(pair.Value);
                }
            }

            // Since there could be multiple chutzpah.json files each setting their own success percentage
            // we take the minimum of all of them. In the future, it would be nice to e able to apply each percentage to the 
            // test files they came from.
            if (!SuccessPercentage.HasValue)
            {
                SuccessPercentage = coverageData.SuccessPercentage;
            }
            else if (coverageData.SuccessPercentage.HasValue)
            {
                SuccessPercentage = Math.Min(SuccessPercentage.Value, coverageData.SuccessPercentage.Value);
            }
        }
    }

    /// <summary>
    /// Contains coverage data for a specific file.
    /// </summary>
    public class CoverageFileData
    {

        private double? coveragePercentage;
        private Tuple<double, double> coveredCount;

        public CoverageFileData()
        {
            this.LineExecutionCounts = new int?[0];
            this.SourceLines = new string[0];
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="coverageFileData"></param>
        public CoverageFileData(CoverageFileData coverageFileData)
        {
            this.FilePath = coverageFileData.FilePath;
            this.LineExecutionCounts = coverageFileData.LineExecutionCounts.ToArray();
            this.SourceLines = coverageFileData.SourceLines.ToArray();
        }

        /// <summary>
        /// The path to the file. Mostly for convenience.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Contains line execution counts for all source lines in the file. The array
        /// is 1-based, which means that the first item is always <c>null</c>. Lines not 
        /// considered executable by the coverage engine also have <c>null</c> as their
        /// values.
        /// </summary>
        public int?[] LineExecutionCounts { get; set; }

        /// <summary>
        /// Contains the converted source code of the test file.
        /// Unlike the <see cref="LineExecutionCounts"/> array, this array
        /// is 0-based, which means that the first item is the first line of the file.
        /// </summary>
        public string[] SourceLines { get; set; }

        /// <summary>
        /// The percentage of line that were covered
        /// </summary>
        public double CoveragePercentage
        {
            get
            {
                if (!coveragePercentage.HasValue)
                {
                    coveragePercentage = CalculateCoveragePercentage();
                }

                return coveragePercentage.Value;
            }
        }

        private double CalculateCoveragePercentage()
        {
            if (LineExecutionCounts == null || LineExecutionCounts.Length == 0)
            {
                return 0;
            }

            var pair = GetCoveredCount();

            return pair.Item1 / pair.Item2;
        }

        public Tuple<double, double> GetCoveredCount()
        {
            if (coveredCount != null) return coveredCount;

            if (LineExecutionCounts == null || LineExecutionCounts.Length == 0)
            {
                coveredCount = Tuple.Create(0.0, 0.0);
                return coveredCount;
            }


            double sum = 0;
            double count = 0;

            for (var i = 1; i < LineExecutionCounts.Length; i++)
            {
                if (LineExecutionCounts[i].HasValue)
                {
                    count++;

                    if (LineExecutionCounts[i] > 0)
                    {
                        sum++;
                    }
                }
            }

            coveredCount = Tuple.Create(sum, count);
            return coveredCount;
        }

        public void Merge(CoverageFileData coverageFileData)
        {
            // If LineExecutionCounts is null then this class has not be merged with any coverage object yet so just take its values
            if (LineExecutionCounts == null)
            {
                LineExecutionCounts = coverageFileData.LineExecutionCounts;
                SourceLines = coverageFileData.SourceLines;
            }
            else
            {
                for (var i = 0; i < LineExecutionCounts.Length; i++)
                {
                    if (!coverageFileData.LineExecutionCounts[i].HasValue)
                    {
                        // No data to merge
                        continue;
                    }
                    else if (!this.LineExecutionCounts[i].HasValue)
                    {
                        // Just take the given data
                        this.LineExecutionCounts[i] = coverageFileData.LineExecutionCounts[i];
                    }
                    else
                    {
                        // If we both have values sum them up
                        this.LineExecutionCounts[i] += coverageFileData.LineExecutionCounts[i];
                    }
                }
            }

            // After update LineExecutionCounts we need recalculate CoveragePercentage
            this.coveragePercentage = null;
        }
    }
}
