namespace Shop.Application.Notifications;

public interface IStoreEmailContent { Task<Dictionary<string, string>> ReadTranslationsAsync(); }
