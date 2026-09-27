# BenchmarkEmail Integration for nopCommerce

This plugin provides a seamless integration between nopCommerce and BenchmarkEmail, allowing you to automate your email marketing efforts directly from your store.

## Features

- **Newsletter Synchronization**: Automatically syncs nopCommerce newsletter subscribers to a specified BenchmarkEmail contact list.
- **Blog Post Automation**: Creates and schedules a BenchmarkEmail campaign for every new blog post created in your store, keeping your subscribers updated with your latest content.

## Requirements

- **nopCommerce Version**: 4.90
- **BenchmarkEmail Account**: A valid BenchmarkEmail account with API access.

## Installation

### Option 1: Using Binaries (Quick Install)
1. Locate the `Misc.Benchmarkemail` folder (contained within the provided ZIP or repository).
2. Upload the `Misc.Benchmarkemail` folder directly into the `\Plugins` directory of your nopCommerce installation.
3. Restart your application.

### Option 2: From Source
1. Build the project using the provided `.csproj` file in `Nop.Plugin.Misc.Benchmarkemail`.
2. Deploy the resulting binaries to the `\Plugins\Misc.Benchmarkemail` directory.

## Configuration

Once installed, navigate to **Administration > Configuration > Plugins** and find the **BenchmarkEmail** plugin.

### Setup Steps:
1. **API Token**: Enter your BenchmarkEmail API token to authenticate the connection.
2. **Contact List**: 
   - Click the "Load lists" button to fetch your existing lists from BenchmarkEmail.
   - Select the target **List ID** where newsletter subscribers should be added.
3. **Blog Post Emails (Optional)**:
   - Enable **Email on Blog Post**.
   - Provide the **Email Template ID** of the BenchmarkEmail template you wish to copy for new blog posts.
   - Set the **Email Schedule Delay** (in minutes) to determine when the email should be sent after a blog post is published.

## Troubleshooting

- **Logs**: Enable the `Log Requests` setting in the plugin configuration to capture detailed API communication for debugging.
- **API Errors**: Ensure your API token is correct and that the selected List ID exists in your BenchmarkEmail account.

---
*This is an open-source integration designed to enhance the marketing capabilities of nopCommerce stores.*
