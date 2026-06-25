import { test, expect } from '@playwright/test';

const MOCK_CITIES = [
  { cityId: 'city-mum', cityName: 'Mumbai' },
  { cityId: 'city-pun', cityName: 'Pune' },
  { cityId: 'city-del', cityName: 'Delhi' },
];

const MOCK_SCHEDULES = [
  {
    scheduleId: 'sch-1',
    busId: 'bus-1',
    busName: 'Express 101',
    busNumber: 'MH-01-AB-1234',
    busType: 'Seater',
    routeId: 'route-1',
    source: 'Mumbai',
    destination: 'Pune',
    travelDate: '2026-06-25',
    departureTime: '08:00',
    arrivalTime: '12:00',
    availableSeats: 10,
    totalSeats: 40,
    isActive: true,
    minSeatPrice: 399,
  },
];

test.describe('Home page', () => {
  test.beforeEach(async ({ page }) => {
    // Intercept API calls so tests run without the backend
    await page.route('**/api/v1/cities', route =>
      route.fulfill({ json: MOCK_CITIES })
    );
    await page.route('**/api/v1/schedules/search**', route =>
      route.fulfill({ json: MOCK_SCHEDULES })
    );
    await page.goto('/');
  });

  test('renders the hero section headline', async ({ page }) => {
    await expect(page.getByRole('heading', { name: /Travel Smarter/i, level: 1 })).toBeVisible();
  });

  test('renders the search form with all fields', async ({ page }) => {
    await expect(page.locator('#from')).toBeVisible();
    await expect(page.locator('#to')).toBeVisible();
    await expect(page.locator('#date')).toBeVisible();
    await expect(page.getByRole('button', { name: /Search Buses/i })).toBeVisible();
  });

  test('cities are loaded into the From and To dropdowns', async ({ page }) => {
    await expect(page.locator('#from option[value="city-mum"]')).toHaveText('Mumbai');
    await expect(page.locator('#to option[value="city-pun"]')).toHaveText('Pune');
  });

  test('renders the Why Choose BusBooking section', async ({ page }) => {
    await expect(page.getByRole('heading', { name: /Why Choose BusBooking/i })).toBeVisible();
    await expect(page.getByText('Wide Coverage')).toBeVisible();
    await expect(page.getByText('Secure Payments')).toBeVisible();
    await expect(page.getByText('Instant Confirmation')).toBeVisible();
  });

  test('renders the FAQ section', async ({ page }) => {
    await expect(page.getByRole('heading', { name: /Frequently Asked Questions/i })).toBeVisible();
    await expect(page.getByText('How do I book a bus ticket?')).toBeVisible();
  });

  test('renders the footer with brand name', async ({ page }) => {
    await expect(page.getByText('🚌 BusBooking')).toBeVisible();
    await expect(page.getByText(/support@busbooking.in/i)).toBeVisible();
  });
});

test.describe('Search flow', () => {
  test.beforeEach(async ({ page }) => {
    await page.route('**/api/v1/cities', route =>
      route.fulfill({ json: MOCK_CITIES })
    );
    await page.route('**/api/v1/schedules/search**', route =>
      route.fulfill({ json: MOCK_SCHEDULES })
    );
    await page.goto('/');
  });

  test('submitting the search form navigates to /search', async ({ page }) => {
    await page.locator('#from').selectOption('city-mum');
    await page.locator('#to').selectOption('city-pun');
    await page.locator('#date').fill('2026-06-25');
    await page.getByRole('button', { name: /Search Buses/i }).click();

    await expect(page).toHaveURL(/\/search\?/);
  });
});
