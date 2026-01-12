# ![Slackord Logo](https://i.imgur.com/PyVjqzL.png)Slackord
![Discord Shield](https://discordapp.com/api/guilds/1095636526873972766/widget.png?style=shield)    
[Join the Slackord Discord!](https://discord.gg/yccMweYPN8)

Slackord is an application that parses JSON chat history file exports from Slack and posts them into Discord instantly with a single command.

# Demo
https://github.com/user-attachments/assets/afa63789-0904-4200-8bd8-6c5dd1970355

# Features
1. `Cross Platform` - Slackord works on both Windows and Mac (Apple Silicon and Intel).
2. `Recreate channels` - Slackord will automatically recreate all your Slack channels on Discord along with their descriptions.
3. `Resume Functionality` - Slackord can resume partial server and channel imports.
4. `Batch and Single channel compatibility` - Choose between importing a single channel or your entire Slack server into Discord.
5. `Output window` - Slackord has a log window that keeps you updated on what's happening in real-time.
6. `Progress bar` - Slackord has a progress bar that keeps you updated on parsing and posting progress.
7. `Slack-to-Discord markdown` - Slackord will convert messages with Slack markdown to Discord markdown.
8. `Auto rate-limit detection` - Slackord limits the messages it sends over time so it doesn't spam or get itself squelched for posting too often.
9. `Ease-of-use` - Slackord only needs to be set up once, making future imports easy. It also remembers your bot's token.
10. `Multi-Server Compatibility` - Join Slackord into a test server and your live server at the same time, and test your imports.
11. `Privacy & Data Safety` - Slackord allows you to set the name format and fallback when posting messages. Choose between `display name`, `user name`, and `real name`. In addition to user privacy, your data is yours. Slackord works completely off your local connection and machine.
12. `Update checks` - Slackord allows you to toggle the ability to get notified when a new version is available.
13. `Active development` - Slackord is actively maintained with consistent quality-of-life fixes, bugfixes, and new features.
14. `Slackdump Compatibility` - Slackord supports Slackdump exports [Slackdump](https://github.com/rusq/slackdump).

# Slackord Does Not Support
1. Private Direct Messages - Not supported due to user privacy concerns.
2. Reactions - A bot cannot give a post ten thumbs up emojis. These actions are done by individuals.
3. Mapping Slack users to Discord users - Support for this may be considered if sponsored.

# Getting Started
1. Create a Discord bot [here](https://discord.com/developers/applications) by selecting "New Application" at the top-right.
2. Name your bot "Slackord", or any preferred custom name.
3. On the "Installation" page, select "Guild Install", and select "None" for the "Install Link" and save your changes.
4. Select "Bot" from the left panel. Ensure that both "Public Bot", and "Requires OAuth2 Code Grant" are both toggled off in Authorization Flow, and "Message Content Intent" is toggled on. then save your changes.
5. Select "OAuth2" from the left panel. Set the bot's "SCOPES" to "bot". This opens a new menu called "BOT PERMISSIONS" below. Set the bot's permissions to "Administrator".
6. Copy the "GENERATED URL" link below the "BOT PERMISSIONS", and paste into a browser. Join the bot into your desired Discord server.
7. Select "Bot" from the left panel. Click "Reset Token", select "Yes, do it!", then click the "Copy" button to the left of "Regenerate". "Keep it secret, keep it safe."

# Running Slackord
1. Run Slackord. You may need to install the latest .NET10 framework [here](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-10.0.1-windows-x64-installer).
2. Click `Options` and paste your Discord bot's token into the `Bot Token` text field.
3. Click `Connect` to connect the bot to your server.
4. Click `Import Server` and select the Slack JSON chat history root folder, or `Import Channel` and select the channel folder.
5. Once parsing has completed, visit any Discord channel you wish and type `/slackord`. Messages will begin posting and the progress bar will update as messages are sent.
6. At any point, you can disconnect or close Slackord and later resume your import. To resume an import, use the `/slackord` or `/resume` slash command when prompted or if Slackord is still running, after reconnecting the bot to Discord.

# Need Help?
1. Visit the [Documentation](https://thomasloupe.github.io/Slackord/docs/) which is updated automatically on main branch commits.
2. Please join the Discord community listed at the top of this page, or open a new issue if it doesn't already exist.
3. If you'd like to donate, you can do so from within Slackord by opening the `Options` page and clicking the `Donate` button at the bottom of the page, sponsoring Slackord at the top of this repository by clicking the heart (sponsor) button, or donating directly to me via [PayPal](https://paypal.me/thomasloupe).

# Special Mentions
While Slackord handles migrating your Slack, archiving your Slack history is something you might also want to do. Check out [Slackdump](https://github.com/rusq/slackdump) for archiving your entire Slack workspace! Slackord officially supports Slackdump exports.
