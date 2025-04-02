import { NextApiRequest, NextApiResponse } from 'next';

// Configure API route options
export const config = {
    api: {
        responseLimit: false,
        bodyParser: {
            sizeLimit: '4mb',
        },
        // Increase timeout to 60 seconds
        externalResolver: true,
    },
};

// Implement API route with caching and error handling
export default async function handler(
    req: NextApiRequest,
    res: NextApiResponse
) {
    if (req.method !== 'GET') {
        return res.status(405).json({ error: 'Method not allowed' });
    }

    try {
        // Enable CORS
        res.setHeader('Access-Control-Allow-Origin', '*');
        res.setHeader('Access-Control-Allow-Methods', 'GET');
        res.setHeader('Access-Control-Allow-Headers', 'Content-Type');

        // Get pagination parameters
        const page = Number(req.query.page) || 1;
        const pageSize = Math.min(Number(req.query.pageSize) || 10, 100);
        const startTime = req.query.startTime as string;
        const endTime = req.query.endTime as string;

        // Construct API URL with query parameters
        const apiUrl = new URL(`${process.env.API_BASE_URL}/prediction-data`);
        apiUrl.searchParams.append('page', page.toString());
        apiUrl.searchParams.append('pageSize', pageSize.toString());
        if (startTime) apiUrl.searchParams.append('startTime', startTime);
        if (endTime) apiUrl.searchParams.append('endTime', endTime);

        // Fetch data with timeout
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), 30000); // 30 second timeout

        const response = await fetch(apiUrl.toString(), {
            signal: controller.signal,
            headers: {
                'Accept-Encoding': 'gzip, deflate, br',
                'Cache-Control': 'no-cache',
            },
        });

        clearTimeout(timeoutId);

        if (!response.ok) {
            throw new Error(`API responded with status: ${response.status}`);
        }

        // Get the data
        const data = await response.json();

        // Set cache headers
        res.setHeader('Cache-Control', 'public, s-maxage=300, stale-while-revalidate=59');

        // Return the response
        return res.status(200).json(data);
    } catch (error: any) {
        console.error('Error fetching prediction data:', error);

        if (error.name === 'AbortError') {
            return res.status(504).json({ error: 'Request timeout' });
        }

        return res.status(500).json({ error: 'Failed to fetch prediction data' });
    }
} 