import { NextApiRequest, NextApiResponse } from 'next';
import { HubConnectionBuilder } from '@microsoft/signalr';

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

// Create SignalR connection
const connection = new HubConnectionBuilder()
    .withUrl(`${process.env.API_BASE_URL}/sportMatchHub`)
    .withAutomaticReconnect()
    .build();

// Store the latest prediction data
let latestPredictionData: any = null;

// Set up SignalR event handlers
connection.on('ReceivePredictionData', (data) => {
    latestPredictionData = data;
});

connection.on('Error', (error) => {
    console.error('SignalR error:', error);
});

// Start the connection
connection.start().catch(err => console.error('SignalR connection error:', err));

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

        // If we have cached data, return it
        if (latestPredictionData) {
            // Apply pagination to the cached data
            const startIndex = (page - 1) * pageSize;
            const endIndex = startIndex + pageSize;
            const paginatedData = {
                ...latestPredictionData,
                data: latestPredictionData.data.slice(startIndex, endIndex),
                pagination: {
                    ...latestPredictionData.pagination,
                    currentPage: page,
                    pageSize: pageSize,
                    totalItems: latestPredictionData.data.length,
                    totalPages: Math.ceil(latestPredictionData.data.length / pageSize),
                    hasNext: endIndex < latestPredictionData.data.length,
                    hasPrevious: page > 1
                }
            };

            // Set cache headers
            res.setHeader('Cache-Control', 'public, s-maxage=300, stale-while-revalidate=59');
            return res.status(200).json(paginatedData);
        }

        // If no cached data, request it from the hub
        await connection.invoke('RequestPredictionData');

        // Wait for data with timeout
        const timeout = 30000; // 30 seconds
        const startTime = Date.now();
        while (!latestPredictionData && Date.now() - startTime < timeout) {
            await new Promise(resolve => setTimeout(resolve, 100));
        }

        if (!latestPredictionData) {
            return res.status(504).json({ error: 'Request timeout' });
        }

        // Apply pagination and return
        const startIndex = (page - 1) * pageSize;
        const endIndex = startIndex + pageSize;
        const paginatedData = {
            ...latestPredictionData,
            data: latestPredictionData.data.slice(startIndex, endIndex),
            pagination: {
                ...latestPredictionData.pagination,
                currentPage: page,
                pageSize: pageSize,
                totalItems: latestPredictionData.data.length,
                totalPages: Math.ceil(latestPredictionData.data.length / pageSize),
                hasNext: endIndex < latestPredictionData.data.length,
                hasPrevious: page > 1
            }
        };

        // Set cache headers
        res.setHeader('Cache-Control', 'public, s-maxage=300, stale-while-revalidate=59');
        return res.status(200).json(paginatedData);
    } catch (error: any) {
        console.error('Error fetching prediction data:', error);
        return res.status(500).json({ error: 'Failed to fetch prediction data' });
    }
} 