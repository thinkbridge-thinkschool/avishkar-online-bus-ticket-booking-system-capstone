import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ScheduleService } from '../../core/services/schedule.service';
import { CityService } from '../../core/services/city.service';
import { AuthService } from '../../core/services/auth.service';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import type { Schedule } from '../../shared/models/schedule.model';

@Component({
  selector: 'app-search-results',
  imports: [RouterLink, LoadingSpinnerComponent],
  templateUrl: './search-results.html',
  styleUrl: './search-results.css',
})
export class SearchResultsComponent implements OnInit {
  private readonly route = inject(ActivatedRoute); // Reads route/query parameters from the URL.
  private readonly router = inject(Router);
  private readonly scheduleService = inject(ScheduleService);
  private readonly cityService = inject(CityService);
  readonly auth = inject(AuthService);

  readonly schedules = signal<Schedule[]>([]); // Stores the list of schedules received from the backend.
  readonly loading = signal(true);
  readonly error = signal<string | null>(null); // Stores an error message if an API call fails.
  readonly fromCityName = signal('');
  readonly toCityName = signal('');
  readonly travelDate = signal('');

  readonly sortBy = signal<'departure' | 'price'>('departure'); // Stores the current sorting option. intially departure time is selected. User can change it to price by clicking the sort button.
  readonly typeFilter = signal<string>(''); // Stores the selected bus type filter. AC Sleeper, Semi-Sleeper, Seater. Initially no filter is applied. User can select a filter from the dropdown.

  readonly filteredSchedules = computed(() => { // whenever Sort changes Angular automatically recalculates the list.
    let list = this.schedules(); // Read the current schedule list.
    const f = this.typeFilter(); // Read filter value like AC Sleeper selected
    if (f) list = list.filter(s => s.busType === f); // If the user selected a bus type, keep only matching buses. Like only AC buse
    if (this.sortBy() === 'price') {
      return [...list].sort((a, b) => (a.minSeatPrice ?? 9999) - (b.minSeatPrice ?? 9999)); // sort by price, if price is null, treat it as 9999
    }
    return [...list].sort((a, b) => a.departureTime.localeCompare(b.departureTime)); // sort by departure time
  });

  async ngOnInit(): Promise<void> {
    const p = this.route.snapshot.queryParams;    // reads the value from home.ts router.navigate(['/search'],{   // url comes here
    this.travelDate.set(p['travelDate'] ?? ''); // Angular stores the travel date in a signal. Date could be required later for filtering or displaying in the search-results.html file.
    try {
      const [cities, results] = await Promise.all([
        this.cityService.getCities(),           
        this.scheduleService.searchSchedules({     // returns BUS A, Bus B.
          fromCityId: p['fromCityId'],
          toCityId: p['toCityId'],
          travelDate: p['travelDate'],
        }),
      ]);
      this.fromCityName.set(cities.find(c => c.cityId === p['fromCityId'])?.cityName ?? p['fromCityId']); // // why city again bcz only city IDs are passed from the Home page.We need city name to display.
      this.toCityName.set(cities.find(c => c.cityId === p['toCityId'])?.cityName ?? p['toCityId']);
      this.schedules.set(results);    // The HTML displays this list. Bus and schedule details are displayed in the search-results.html file.
    } catch (err: unknown) {
      this.error.set((err as Error).message);
    } finally {
      this.loading.set(false);
    }
  }

  book(s: Schedule): void {   // Suppose user clicks, Book Now Angular executes to schedule
    if (!this.auth.isAuthenticated()) {
      this.auth.login(); // If user is not logged in -> goes to Login Page
      return;
    }
    this.router.navigate(['/book', s.scheduleId], {      // If user is logged in Angular navigates to /book/{scheduleId}.
      queryParams: {
        source: s.source,
        destination: s.destination,
        busName: s.busName,
        busNumber: s.busNumber,
        travelDate: s.travelDate,
        departureTime: s.departureTime,
        arrivalTime: s.arrivalTime,
        minSeatPrice: s.minSeatPrice,
      },
    });
  }

  formatTime(t: string): string {
    const [h, m] = t.split(':').map(Number);
    const suffix = h >= 12 ? 'PM' : 'AM';
    const hour12 = h % 12 || 12;
    return `${hour12}:${String(m).padStart(2, '0')} ${suffix}`;
  }

  formatTravelDate(d: string): string {
    return new Date(d + 'T00:00:00').toLocaleDateString('en-IN', {
      weekday: 'long', day: 'numeric', month: 'long', year: 'numeric',
    });
  }

  duration(dep: string, arr: string): string {
    const [dh, dm] = dep.split(':').map(Number);
    const [ah, am] = arr.split(':').map(Number);
    let depMins = dh * 60 + dm;
    let arrMins = ah * 60 + am;
    if (arrMins <= depMins) arrMins += 24 * 60; // overnight
    const diff = arrMins - depMins;
    const h = Math.floor(diff / 60);
    const m = diff % 60;
    return m > 0 ? `${h}h ${m}m` : `${h}h`;
  }

  busTypeLabel(t: string): string {
    if (t === 'Sleeper') return 'AC Sleeper';
    if (t === 'SemiSleeper') return 'Semi-Sleeper';
    return 'Seater';
  }
}
