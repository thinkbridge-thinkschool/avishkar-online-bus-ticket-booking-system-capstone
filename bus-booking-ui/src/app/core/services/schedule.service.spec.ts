import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { ScheduleService } from './schedule.service';
import type { Schedule, SearchSchedulesRequest } from '../../shared/models/schedule.model';

const MOCK_SCHEDULE: Schedule = {
  scheduleId: 'sch-1',
  busId: 'bus-1',
  busName: 'Express 101',
  busNumber: 'MH-01-AB-1234',
  busType: 'Seater',
  routeId: 'route-1',
  source: 'Mumbai',
  destination: 'Pune',
  travelDate: '2026-06-24',
  departureTime: '08:00',
  arrivalTime: '12:00',
  availableSeats: 2,
  totalSeats: 40,
  isActive: true,
  minSeatPrice: 399,
};

describe('ScheduleService', () => {
  let service: ScheduleService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        ScheduleService,
      ],
    });
    service = TestBed.inject(ScheduleService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('searchSchedules should call GET /api/v1/schedules/search with params', async () => {
    const params: SearchSchedulesRequest = {
      fromCityId: 'city-a',
      toCityId: 'city-b',
      travelDate: '2026-06-24',
    };

    const promise = service.searchSchedules(params);

    const req = httpMock.expectOne(r =>
      r.method === 'GET' && r.url === '/api/v1/schedules/search'
    );
    expect(req.request.params.get('fromCityId')).toBe('city-a');
    expect(req.request.params.get('toCityId')).toBe('city-b');
    expect(req.request.params.get('travelDate')).toBe('2026-06-24');

    req.flush([MOCK_SCHEDULE]);
    const result = await promise;
    expect(result.length).toBe(1);
    expect(result[0].scheduleId).toBe('sch-1');
  });

  it('getById should call GET /api/v1/schedules/:id', async () => {
    const promise = service.getById('sch-1');

    const req = httpMock.expectOne('/api/v1/schedules/sch-1');
    expect(req.request.method).toBe('GET');
    req.flush(MOCK_SCHEDULE);

    const result = await promise;
    expect(result.busName).toBe('Express 101');
  });
});
