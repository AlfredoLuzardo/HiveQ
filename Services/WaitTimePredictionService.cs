using HiveQ.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HiveQ.Services
{
    public interface IWaitTimePredictionService
    {
        Task<int> CalculateEstimatedWaitTimeAsync(int queueId, int positionInQueue);
        Task UpdateAllQueueWaitTimesAsync(int queueId);
    }

    public class WaitTimePredictionService : IWaitTimePredictionService
    {
        private readonly ApplicationDbContext _context;

        public WaitTimePredictionService(ApplicationDbContext context)
        {
            _context = context;
        }

        // This simulates the "AI" component: Analyzing history to predict the future
        public async Task<int> CalculateEstimatedWaitTimeAsync(int queueId, int positionInQueue)
        {
            if (positionInQueue <= 0) return 0;

            // 1. Fetch recent history to determine service velocity
            // We look at the last 10 completed entries to see how fast the line moves
            var recentHistory = await _context.QueueHistories
                .Where(h => h.QueueId == queueId && h.Status == "Completed" && h.ServedAt != null)
                .OrderByDescending(h => h.ServedAt)
                .Take(10)
                .ToListAsync();

            double averageServiceTimeMinutes = 5.0; // Default fallback

            if (recentHistory.Count >= 2)
            {
                // Calculate time difference between the first and last person in this batch being served
                var newest = recentHistory.First().ServedAt!.Value;
                var oldest = recentHistory.Last().ServedAt!.Value;
                
                var totalDuration = (newest - oldest).TotalMinutes;
                var servedCount = recentHistory.Count - 1; // Intervals between people

                if (servedCount > 0)
                {
                    averageServiceTimeMinutes = totalDuration / servedCount;
                }
            }

            // 2. Calculate Estimate
            // Estimate = AvgTimePerPerson * PeopleAhead
            int estimatedMinutes = (int)Math.Ceiling(averageServiceTimeMinutes * positionInQueue);

            // Optional: Add buffer for "AI" variance (e.g. time of day adjustments could go here)
            return estimatedMinutes;
        }

        public async Task UpdateAllQueueWaitTimesAsync(int queueId)
        {
            // 1. Get all waiting entries for this queue
            var activeEntries = await _context.QueueEntries
                .Where(qe => qe.QueueId == queueId && qe.Status == "Waiting")
                .OrderBy(qe => qe.PositionNumber)
                .ToListAsync();

            // 2. Update each entry
            for (int i = 0; i < activeEntries.Count; i++)
            {
                var entry = activeEntries[i];
                // Position is i + 1 because i is 0-indexed
                int prediction = await CalculateEstimatedWaitTimeAsync(queueId, i + 1);
                
                entry.EstimatedWaitTime = prediction; //
                entry.NotifiedAt = DateTime.UtcNow; // Mark that we updated/checked it
            }

            // 3. Save changes to Database
            await _context.SaveChangesAsync();
        }
    }
}