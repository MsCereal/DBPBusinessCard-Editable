using DBPBusinessCardEditable.Models;
using System;
using System.Collections.Concurrent;

namespace DBPBusinessCardEditable.Services
{
    /// <summary>
    /// In-memory store keyed by userId. Each user gets their own permanent card profile.
    /// </summary>
    public class CardProfileService
    {
        private readonly ConcurrentDictionary<string, CardProfile> _store
            = new ConcurrentDictionary<string, CardProfile>(StringComparer.OrdinalIgnoreCase);

        public CardProfile GetOrCreate(string userId)
        {
            return _store.GetOrAdd(userId, id => new CardProfile { UserId = id });
        }

        public CardProfile Get(string userId)
        {
            _store.TryGetValue(userId, out var profile);
            return profile;
        }

        public void Save(CardProfile profile)
        {
            profile.LastUpdated = DateTime.UtcNow;
            _store[profile.UserId] = profile;
        }

        public void Reset(string userId)
        {
            _store[userId] = new CardProfile { UserId = userId };
        }
    }
}
