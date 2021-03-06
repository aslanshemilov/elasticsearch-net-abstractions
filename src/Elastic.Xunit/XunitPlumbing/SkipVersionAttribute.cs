using System;
using System.Collections.Generic;
using System.Linq;
using SemVer;

namespace Elastic.Xunit.XunitPlumbing
{
	/// <summary>
	/// An Xunit test that should be skipped for given Elasticsearch versions, and a reason why.
	/// </summary>
	public class SkipVersionAttribute : Attribute
	{
		/// <summary>
		/// The reason why the test should be skipped
		/// </summary>
		public string Reason { get; }

		/// <summary>
		/// The version ranges for which the test should be skipped
		/// </summary>
		public IList<Range> Ranges { get; }

		// ReSharper disable once UnusedParameter.Local
		// reason is used to allow the test its used on to self document why its been put in place
		public SkipVersionAttribute(string skipVersionRangesSeparatedByComma, string reason)
		{
			Reason = reason;
			this.Ranges = string.IsNullOrEmpty(skipVersionRangesSeparatedByComma)
				? new List<Range>()
				: skipVersionRangesSeparatedByComma.Split(',')
					.Select(r => r.Trim())
					.Where(r => !string.IsNullOrWhiteSpace(r))
					.Select(r => new Range(r))
					.ToList();
		}
	}
}
