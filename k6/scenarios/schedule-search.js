// Hits the hottest anonymous, cache-optimized read path (OutputCache + HybridCache layered
// underneath — see SearchSchedulesHandler). Requires two real city GUIDs and a travel date
// that has a seeded/created schedule; pass them via env vars.
// Run: k6 run k6/scenarios/schedule-search.js -e BASE_URL=https://localhost:7174 \
//   -e FROM_CITY_ID=<guid> -e TO_CITY_ID=<guid> -e TRAVEL_DATE=2026-08-01
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
const FROM_CITY_ID = __ENV.FROM_CITY_ID;
const TO_CITY_ID = __ENV.TO_CITY_ID;
const TRAVEL_DATE = __ENV.TRAVEL_DATE;

export default function () {
  const url = `${BASE_URL}/api/v1/schedules/search?fromCityId=${FROM_CITY_ID}&toCityId=${TO_CITY_ID}&travelDate=${TRAVEL_DATE}`;
  const res = http.get(url);
  check(res, { 'status is 200': (r) => r.status === 200 });
}
