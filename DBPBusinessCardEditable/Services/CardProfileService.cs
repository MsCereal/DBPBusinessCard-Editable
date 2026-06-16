using DBPBusinessCardEditable.Models;
using System;
using System.Collections.Concurrent;

namespace DBPBusinessCardEditable.Services
{
    /// <summary>
    /// In-memory store keyed by Employee ID.
    /// Each employee's card is permanent and unique.
    /// </summary>
    public class CardProfileService
    {
        private readonly ConcurrentDictionary<string, CardProfile> _store
            = new ConcurrentDictionary<string, CardProfile>(StringComparer.OrdinalIgnoreCase);

        public CardProfile Get(string empId)
        {
            _store.TryGetValue(empId.Trim(), out var profile);
            return profile;
        }

        public CardProfile GetOrCreate(string empId)
        {
            return _store.GetOrAdd(empId.Trim(), id => new CardProfile { EmpId = id });
        }

        public void Save(CardProfile profile)
        {
            profile.EmpId = profile.EmpId.Trim();
            profile.LastUpdated = DateTime.UtcNow;
            _store[profile.EmpId] = profile;
        }

        public void Reset(string empId)
        {
            _store[empId.Trim()] = new CardProfile { EmpId = empId.Trim() };
        }
    }
}
