using System;
using System.Collections.Generic;
using ScholarwaveCBTApp.Models;

namespace ScholarwaveCBTApp.Jamb
{
	public class JambSubjectSession
	{
		public string SubjectTitle { get; set; } = string.Empty;
		public List<Question> Questions { get; set; } = new List<Question>();
		public int CurrentIndex { get; set; } = 0;
		public int[] StudentAnswers { get; set; } = Array.Empty<int>();
		public bool[] Flagged { get; set; } = Array.Empty<bool>();
	}

	public class JambExamSession
	{
		public List<JambSubjectSession> Subjects { get; set; } = new List<JambSubjectSession>(4);
		public int TotalSecondsRemaining { get; set; } = 120 * 60; // Fixed 120 minutes
		public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
	}
}
