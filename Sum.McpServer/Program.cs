using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.Threading.RateLimiting;

namespace Sum.Mcp.Server
{
    /// <summary>
    /// The entry point of the application.
    /// </summary>
    public static class Program
    {
        /*   

            Welcome to Sum Matrix Protocol Server! 

            Sum Matrix is changing the world by connecting people to the latest technologies.
            Our mission is to empower individuals and organizations to leverage cutting-edge tools
            and innovations, making advanced capabilities accessible to everyone.

            This server is your gateway to the Sum Matrix ecosystem, providing seamless integration
            with modern protocols and enabling you to stay ahead in a rapidly evolving tech landscape.

            Get started and join us in shaping the future!

            Quick Start:

            1. Build & Run (Visual Studio):
               - Open the solution file: Sum.Mcp.sln
               - Set 'Sum.Mcp.Server' as the startup project.
               - Run the project.

            2. Configuration (Optional):
               - API Key for remote MCP: Set environment variable MCP__ApiKey=yourapikey (or edit appsettings.json).
               - Redis (for tools): Set REDIS_CONNECTION=hostname:6379,password=yourpassword
                 If unset, tools receive IDatabase? as null (your code should handle this).

            3. HTTP Endpoint:
               - Default: http://localhost:8080/mcp
               - To use a different port, set ASPNETCORE_URLS.

            4. Stdio Host Example Configuration:
               {
                 "mcpServers": {
                   "sum-mcp": {
                     "command": "dotnet",
                     "args": ["run", "--project", "src/Sum.Mcp.Server/Sum.Mcp.Server.csproj"]
                   }
                 }
               }

            5. Terms & License:
               - This repository is provided under the MIT License.

                MIT License - Copyright (c) 2025 Sum Matrix 

                Permission is hereby granted, free of charge, to any person obtaining a copy
                of this software and associated documentation files (the "Software"), to deal
                in the Software without restriction, including without limitation the rights
                to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
                copies of the Software, and to permit persons to whom the Software is
                furnished to do so, subject to the following conditions:

                The above copyright notice and this permission notice shall be included in
                all copies or substantial portions of the Software.

                THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
                IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
                FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
                AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
                LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
                OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
                THE SOFTWARE.

*/


        /// <summary>
        /// The entry point of the application.
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure basic console logging. We clear the default providers to
            // avoid duplicate logs and use a simple timestamped format. This
            // keeps the output easy to read when running under stdio transport.
            builder.Logging.ClearProviders();
            builder.Logging.AddSimpleConsole(options =>
            {
                options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
                options.SingleLine = true;
                options.UseUtcTimestamp = true;
            });

            // Add the MCP server services. The MCP SDK will scan the assembly
            // for classes decorated with McpServerToolType and register all
            // methods annotated with McpServerTool as tools. We also supply
            // metadata about the server itself.
            builder.Services
                .AddMcpServer(options =>
                {
                    options.ServerInfo = new ModelContextProtocol.Protocol.Implementation
                    {
                        Name = "Sum.Mcp", // name displayed to clients
                        Version = "1.0.0"
                    };
                })
                .WithToolsFromAssembly()        // discover tools in this assembly
                .WithStdioServerTransport()     // allow local stdio transport
                .WithHttpTransport();           // enable HTTP transport


            /// <summary>
            /// Configures global rate limiting for the MCP server.
            /// - Uses ASP.NET Core's built-in RateLimiter middleware.
            /// - Each client is partitioned by "X-Api-Key" header (or "anon" if missing).
            /// - Sliding window algorithm: up to 10 requests per 10 seconds.
            /// - Requests exceeding the limit are rejected immediately with HTTP 429.
            /// </summary>
            builder.Services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                {
                    var key = ctx.Request.Headers.TryGetValue("X-Api-Key", out var v) && !string.IsNullOrWhiteSpace(v)
                        ? v.ToString()
                        : "anon";

                    return RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: key,
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromSeconds(10),
                            SegmentsPerWindow = 10,
                            QueueLimit = 0
                        });
                });

                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            });

            // Read the API key from configuration (environment or appsettings).
            var apiKey = builder.Configuration["MCP:ApiKey"];

            var app = builder.Build();

            app.UseRateLimiter();

            // Middleware to protect the HTTP transport with an API key. If a
            // client sends a request to the "/mcp" endpoint without the
            // correct X-Api-Key header, the request is rejected. When no API
            // key is configured, the endpoint is open. For streamable HTTP
            // connections, the header must be present on the initial request.
            if (!string.IsNullOrEmpty(apiKey))
            {
                app.Use(async (context, next) =>
                {
                    // Only protect the MCP endpoint
                    if (context.Request.Path.StartsWithSegments("/mcp"))
                    {
                        if (!context.Request.Headers.TryGetValue("X-Api-Key", out var header) || header != apiKey)
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            await context.Response.WriteAsync("Unauthorized");
                            return;
                        }
                    }
                    await next();
                });
            }

            // Map the MCP endpoint. The ASP.NET Core integration provided by
            // ModelContextProtocol.AspNetCore will register the necessary
            // routes and middleware under the provided path. Clients use
            // JSON‑RPC 2.0 over HTTP or streamable transports to invoke tools.
            app.MapMcp("/mcp");

            // Simple health check endpoint. This can be used to verify the
            // process is running and returns a small JSON document.
            app.MapGet("/", () =>
            {
                return Results.Ok(new { status = "ok", server = "Sum.Mcp"});
            });

            app.Run();
        }
    }
}