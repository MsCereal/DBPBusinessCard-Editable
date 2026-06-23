using DBPBusinessCardEditable.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace DBPBusinessCardEditable.Services
{
    /// <summary>
    /// In-memory store keyed by Employee ID.
    /// Data persists for the lifetime of the running process.
    /// Cards survive as long as Railway does not redeploy.
    /// </summary>
    public class CardProfileService
    {
        private readonly ConcurrentDictionary<string, CardProfile> _store
            = new ConcurrentDictionary<string, CardProfile>(StringComparer.OrdinalIgnoreCase);

        public CardProfile Get(string empId)
        {
            if (string.IsNullOrWhiteSpace(empId)) return null;
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

        public void Delete(string empId)
        {
            _store.TryRemove(empId.Trim(), out _);
        }

        public int ClearAll()
        {
            int count = _store.Count;
            _store.Clear();
            return count;
        }

        public int Count() => _store.Count;

        public List<CardProfile> GetAll()
        {
            return _store.Values
                .OrderByDescending(p => p.LastUpdated)
                .ToList();
        }
    }
}
