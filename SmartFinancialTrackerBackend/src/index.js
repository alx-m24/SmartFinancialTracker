const HTML_PAGE = `<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>TUSC Concern Compiler API</title>
</head>
<body>
  <h1>API only worker</h1>
</body>
</html>`;

const CORS_HEADERS = {
    "Access-Control-Allow-Origin": "*",
    "Access-Control-Allow-Methods": "GET, POST, OPTIONS",
    "Access-Control-Allow-Headers": "Content-Type",
};

// Tried in order: the main model twice (transient failures), then the cheaper fallback.
const PRIMARY_MODEL = "@cf/meta/llama-3.3-70b-instruct-fp8-fast";
const FALLBACK_MODEL = "@cf/meta/llama-3.1-8b-instruct-fast";
const MODEL_ATTEMPTS = [PRIMARY_MODEL, PRIMARY_MODEL, FALLBACK_MODEL];
const RETRY_DELAY_MS = 500;

function json(data, status = 200) {
    return new Response(JSON.stringify(data), {
        status,
        headers: {
            "content-type": "application/json",
            ...CORS_HEADERS,
        },
    });
}

const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

async function handleApi(request, env, url) {
    const path = url.pathname;
    const method = request.method;

    // ------------------------------------------------------------
    // POST /api/d1
    // Body:
    // {
    //   "sql": "SELECT * FROM Department WHERE DepartmentID = ?",
    //   "params": [1]
    // }
    // ------------------------------------------------------------

    if (path === "/api/d1" && method === "POST") {
        let body;

        try {
            body = await request.json();
        } catch {
            return json({ error: "Invalid JSON" }, 400);
        }

        if (typeof body.sql !== "string") {
            return json({ error: "Missing or invalid 'sql'" }, 400);
        }

        const params = Array.isArray(body.params)
            ? body.params
            : [];

        try {
            const result = await env.DB
                .prepare(body.sql)
                .bind(...params)
                .all();

            return json(result);
        } catch (error) {
            return json({
                error: "D1 query failed",
                message: error instanceof Error
                    ? error.message
                    : String(error),
            }, 400);
        }
    }

    return json({ error: "Not found" }, 404);
}

export default {
    async fetch(request, env, ctx) {
        const url = new URL(request.url);

        // CORS preflight
        if (request.method === "OPTIONS") {
            return new Response(null, {
                status: 204,
                headers: CORS_HEADERS,
            });
        }

        if (url.pathname.startsWith("/api/")) {
            // Safety net: anything uncaught still returns JSON with CORS headers,
            // so the browser shows the real error instead of a fake CORS failure.
            try {
                return await handleApi(request, env, url);   // await is required here
            } catch (error) {
                return json({
                    error: "Worker error",
                    message: error instanceof Error ? error.message : String(error),
                }, 500);
            }
        }

        return new Response(HTML_PAGE, {
            headers: {
                "content-type": "text/html;charset=UTF-8",
            },
        });
    },
};