import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection, signal } from '@angular/core';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { SearchResultsComponent } from './search-results';
import { ScheduleService } from '../../core/services/schedule.service';
import { CityService } from '../../core/services/city.service';
import { AuthService } from '../../core/services/auth.service';
import type { Schedule } from '../../shared/models/schedule.model';

function makeSchedule(overrides: Partial<Schedule> = {}): Schedule {
  return {
    scheduleId: 'sch-1',
    departureTime: '08:00',
    arrivalTime: '12:00',
    availableSeats: 10,
    busType: 'Seater',
    minSeatPrice: 399,
    ...overrides,
  };
}

describe('SearchResultsComponent – filteredSchedules', () => {
  let component: SearchResultsComponent;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [SearchResultsComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParams: {} } },
        },
        {
          provide: ScheduleService,
          useValue: {
            searchSchedules: jasmine.createSpy().and.returnValue(Promise.resolve([])),
          },
        },
        {
          provide: CityService,
          useValue: {
            getCities: jasmine.createSpy().and.returnValue(Promise.resolve([])),
          },
        },
        {
          provide: AuthService,
          useValue: {
            isAuthenticated: signal(false),
            login: jasmine.createSpy('login'),
            loginLocal: jasmine.createSpy('loginLocal'),
          },
        },
      ],
    });

    const fixture = TestBed.createComponent(SearchResultsComponent);
    component = fixture.componentInstance;
  });

  it('book() sends an unauthenticated user to the local login chooser, not straight to MSAL', () => {
    const auth = TestBed.inject(AuthService);
    component.book(makeSchedule());

    expect(auth.loginLocal).toHaveBeenCalled();
    expect(auth.login).not.toHaveBeenCalled();
  });

  it('filteredSchedules returns all schedules when typeFilter is empty', () => {
    const s1 = makeSchedule({ scheduleId: 'sch-1', busType: 'Seater' });
    const s2 = makeSchedule({ scheduleId: 'sch-2', busType: 'Sleeper' });
    component.schedules.set([s1, s2]);
    component.typeFilter.set('');

    expect(component.filteredSchedules().length).toBe(2);
  });

  it('filteredSchedules filters by busType when typeFilter is set', () => {
    const seater  = makeSchedule({ scheduleId: 's1', busType: 'Seater' });
    const sleeper = makeSchedule({ scheduleId: 's2', busType: 'Sleeper' });
    component.schedules.set([seater, sleeper]);
    component.typeFilter.set('Seater');

    const result = component.filteredSchedules();
    expect(result.length).toBe(1);
    expect(result[0].busType).toBe('Seater');
  });

  it('filteredSchedules sorts by price when sortBy is price', () => {
    const cheap     = makeSchedule({ scheduleId: 'c', departureTime: '10:00', minSeatPrice: 200 });
    const expensive = makeSchedule({ scheduleId: 'e', departureTime: '08:00', minSeatPrice: 500 });
    component.schedules.set([expensive, cheap]);
    component.sortBy.set('price');

    const result = component.filteredSchedules();
    expect(result[0].minSeatPrice).toBe(200);
    expect(result[1].minSeatPrice).toBe(500);
  });

  it('filteredSchedules sorts by departureTime by default', () => {
    const later   = makeSchedule({ scheduleId: 'l', departureTime: '14:00' });
    const earlier = makeSchedule({ scheduleId: 'e', departureTime: '06:00' });
    component.schedules.set([later, earlier]);
    component.sortBy.set('departure');

    const result = component.filteredSchedules();
    expect(result[0].departureTime).toBe('06:00');
  });
});
