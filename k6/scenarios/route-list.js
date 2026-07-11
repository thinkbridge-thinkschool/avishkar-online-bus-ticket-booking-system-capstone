// Second reference point, same shape as city-list.js — requires auth (routes group has
// RequireAuthorization() with no per-route AllowAnonymous), so pass a bearer token via
// TOKEN env var (see k6/README.md for how to obtain one).
// Run: k6 run k6/scenarios/route-list.js -e BASE_URL=https://localhost:7174 -e TOKEN=<jwt>
import http from 'k6/http';
import { check } from 'k6';

export const options = {
  vus: 20,
  duration: '30s',
  thresholds: {
    http_req_duration: ['p(95)<300', 'p(99)<800'],
    http_req_failed: ['rate<0.01'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5084';
const TOKEN = __ENV.TOKEN || '';

export default function () {
  const res = http.get(`${BASE_URL}/api/v1/routes`, {
    headers: TOKEN ? { Authorization: `Bearer ${TOKEN}` } : {},
  });
  check(res, { 'status is 200': (r) => r.status === 200 });
}
