// Baseline/floor-latency scenario — anonymous, trivial payload, no other dependencies.
// Run: k6 run k6/scenarios/city-list.js -e BASE_URL=https://localhost:7174
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

export default function () {
  const res = http.get(`${BASE_URL}/api/v1/cities`);
  check(res, {
    'status is 200': (r) => r.status === 200,
    'body is an array': (r) => Array.isArray(JSON.parse(r.body)),
  });
}
