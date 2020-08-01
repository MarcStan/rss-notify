# Rss Notify

Matrix bot that posts messages whenever rss feeds are updated.

Notifications based on [notification-targets](https://github.com/MarcStan/notification-targets).

# Setup

## Running locally

Create a keyvault and enter its name into `local.settings.json`.

Make sure you are authorized to access the keyvault (e.g. by running [az login](https://docs.microsoft.com/cli/azure/reference-index?view=azure-cli-latest#az-login)).

For local testing a dummy subscription is added (`Subscriptions:0`). Later you should add them to the keyvault.

## Setup Matrix

Posting messages to a matrix room via an account requires you to setup a "bot account" (or use your own account).

Note that this notification sends messages without end-to-end encryption.

When setting up bots, it is common for the bot to require an "access token" in order to work. Access tokens authenticate bots to the server so that they can function. Access tokens should be kept secret and never shared.

1. In a private/incognito browser window, open Element.
2. Log in to the account you want to get the access token for, such as the bot's account.
3. Click on the bot's name in the top left corner then "Settings".
4. (Optional) Set your bot's display name and avatar.
5. Click the "Help & About" tab (left side of the dialog).
6. Scroll to the bottom and click the <click to reveal> part of Access Token: <click to reveal>.
7. Copy your access token to a safe place, like the bot's configuration file.
8. Do not log out. Instead, just close the window. If you used a private browsing session, you should be able to still use Element for your own account. Logging out deletes the access token from the server, making the bot unable to use it.


See also [Getting your access token from Element](https://t2bot.io/docs/access_tokens/).

``` json
{
  "Matrix": {
    "RoomId": "go to room settings -> advanced to find the internal room id",
    "AccessToken": "log into the account, goto settings -> Help & about -> click to reveal (do not log out of the account or the token is revoked)"
  }
}
```

Add these configuration values to the keyvault (`Matrix--RoomId` and `Matrix--AccessToken` respectively) to setup the bot.

Also add `Matrix--TimeZone` and set it whichever timezone you want to receive notifications as (e.g. "Pacific Time (US & Canada)").

See also https://docs.microsoft.com/en-us/dotnet/api/system.timezoneinfo.getsystemtimezones?view=netcore-3.1

### Testing

You should then be able to use the POST endpoint to test sending messages as the bot.

The bot check function should also auto start locally and parse the rss feed for updates. If any are found they will be posted to the channel, too.

The testendpoint accepts a POST request with a [Message](https://github.com/MarcStan/rss-notify/blob/859a624a024885301efd14a662aa5b3959e17cf9/RssNotify.Functions/BotFunctions.cs#L71):

``` json
{
  "subject": "",
  "message": "",
  "roomId": "optional - if set overrides the configured default"
}
```

# Configuration

By default subscriptions are loaded from IConfiguration (which in turn loads app settings and keyvault settings).

Alternative sources can be used by setting the `subscriptionSource` key (either in ARM template or app settings):

``` json
{
  "SubscriptionSource": "storage"
}
```

Currently these sources are supported:

* nothing set or `config` - default behaviour, will load `Subscriptions--X` keys from IConfiguration where X is any number
* `storage` - will look for `config/subscriptions.json` and load subscriptions from said file

## Adding subscriptions to IConfiguration

Once everything is setup you can add subscriptions by creating keyvault entries `Subscription--X` where X is a number (I recommend you start at 0 and count upwards, but interestingly the configuration system ignores skipped values and still correctly maps them to an array).

Each entry should then contain this json structure:

``` json
{
  "type": "rss",
  "url": "https://invidio.us/feed/channel/UCsXVk37bltHxD1rDPwtNM8Q",
  "name": "Kurzgesagt"
}
```

Because keyvault doesn't support multiline you should just flatten it into a single line:

``` json
{"type": "rss", "url": "https://invidio.us/feed/channel/UCsXVk37bltHxD1rDPwtNM8Q", "name": "Kurzgesagt"}
```

You can also use the keyvault content type to set it to a readable name.

On next run the bot should pick up the new rss source and deliver notifications if updates are found (initially it will look for updates in the last 3 days only).

## Adding subscriptions to storage

The file `config/subscriptions.json` must contain an array like so to have subscriptions load from storage:

``` json
[
  {
    "type": "rss",
    "url": "https://invidio.us/feed/channel/UCsXVk37bltHxD1rDPwtNM8Q",
    "name": "Kurzgesagt"
  },
  {
    "type": "rss",
    "url": "https://invidio.us/feed/channel/UCsXVk37bltHxD1rDPwtNM8Q",
    "name": "Kurzgesagt #2"
  }
]
```