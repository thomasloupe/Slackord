# ![Slackord Logo](https://i.imgur.com/PyVjqzL.png)Slackord
![Discord Shield](https://discordapp.com/api/guilds/1095636526873972766/widget.png?style=shield)    
[Join the Slackord Discord!](https://discord.gg/yccMweYPN8)

Slackord is an application that parses JSON chat history file exports from Slack and posts them into Discord instantly with a single command.

# Demo
https://github.com/thomasloupe/Slackord/assets/6563450/adee8003-b51b-4859-b8d4-3a3e8dcc33c2

# Features
1. `Imports channels` - Slackord will automatically recreate all your Slack channels on Discord along with their descriptions.
1. `Batch and Single channel compatibility` - Choose between importing a single channel or your entire Slack server into Discord.
1. `Output window` - Slackord has a log window that keeps you updated on what's happening.
1. `Progress bar` - Slackord has a progress bar that keeps you updated on parsing and posting progress.
1. `Slack-to-Discord markdown` - Slackord will convert messages with Slack markdown to Discord markdown.
1. `Auto rate-limit detection` - Slackord limits the messages it sends over time so it doesn't spam or get itself squelched for posting too often.
1. `Ease-of-use` - Slackord only needs to be set up once, making future imports easy. It also remembers your bot's token.
1. `Multiple Discord Servers` - Slackord can discern which Discord server it's in and can post to the server(s) you choose.
1. `Privacy first` - Slackord allows you to set the name format and fallback when posting messages. Choose between `display name`, `user name`, and `real name`.
1. `Data safety` - Your data is yours. Slackord works completely off your local connection and machine.
1. `Update checks` - Slackord checks for the latest version automatically. You can also check manually with two clicks. Get the latest version with new features and fixes easily!
1. `Active development` - Slackord is actively maintained with consistent quality-of-life fixes, bugfixes, and new features.

# Getting Started
If you're looking for a great way to archive your Slack history, I recommend [Slackdump](https://github.com/rusq/slackdump)! Please keep in mind that Slackord is developed specifically for default Slack exports, that Slackdump is not developed or maintained by myself, and that Slackdump is very likely imcompatible at this point with Slackord. If your Slackdump export doesn't work with Slackord, it's best to use the default Slack export. I cannot offer troubleshooting assistance with Slackdump exports.
1. Download the [latest](https://github.com/thomasloupe/Slackord/releases) Slackord release and extract the contents.
2. Create a Discord bot [here](https://discord.com/developers/applications) by selecting "New Application" at the top-right.
3. Name your bot "Slackord", or any preferred custom name.
4. On the "Installation" page, uncheck "User Install", and select "None" for the "Install Link" and save your changes.
5. Select "Bot" from the left panel, and upload an image for your bot if desired.
6. Select "OAuth2" from the left panel. set the bot's "SCOPES" to "bot". This opens a new menu called "BOT PERMISSIONS" below. In "BOT PERMISSIONS", set the bot's permissions to "Administrator". This allows Slackord to post to private channels, too.
7. Copy the "GENERATED URL" link below the "BOT PERMISSIONS", and paste into a browser.
8. Join the bot into your desired server using the link generated.
9. Click "Reset Token", select "Yes, do it!", then click the "Copy" button to the left of "Regenerate". "Keep it secret, keep it safe."
10. Ensure that both "PUBLIC BOT" and "REQUIRES OAUTH2 CODE GRANT" sliders are turned off.
11. In "Privileged Gateway Intents", tick the slider to enable "MESSAGE CONTENT INTENT".

# Running Slackord
1. Run Slackord.
1. Click `Set Bot Token` and paste the copied token into the popup text field. Slackord will remember your token if you close it.
1. Click `Connect` to connect the bot to your server.
1. Click `Import Server` and select the Slack JSON chat history root folder, or `Import Channel` and select the channel folder.
1. Once parsing has completed, visit any Discord channel you wish and type `/slackord`. Messages will begin posting and the progress bar will update as messages are sent.

# Important: Please Read!
1. If you need help, please feel free to join the Discord community listed at the top of this page, or open a new issue if it doesn't already exist.
1. Slackord is targeted for Windows 11. If you use Windows 10, Slackord may not work the way you desire, or at all. 
1. If you have a very large Slack server to import, it's much better to import a single channel, rather than the entire server. "Large servers" by Slackord's definition would be Slack imports that have more than 400+ JSON files to parse in total, either by single channel or across the entire server.
1. Slackord is free, and it will always be free. However, if you found Slackord worth donating something, you can donate from within Slackord by clicking Donate inside Slackord, or you can sponsor Slackord at the top of this repository by clicking the heart (sponsor) button. You can also [click here](https://paypal.me/thomasloupe) (PayPal) to donate directly to me.
