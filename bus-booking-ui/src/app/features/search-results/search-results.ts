import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { DatePipe } from '@angular/common';
import { ScheduleService } from '../../core/services/schedule.service';
import { CityService } from '../../core/services/city.service';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import type { Schedule } from '../../shared/models/schedule.model';

@Component({
  selector: 'app-search-results',
  imports: [LoadingSpinnerComponent, DatePipe],
  templateUrl: './search-results.html',
  styleUrl: './search-results.css',
})
export class SearchResultsComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly scheduleService = inject(ScheduleService);
  private readonly cityService = inject(CityService);

  readonly schedules = signal<Schedule[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly fromCityName = signal('');
  readonly toCityName = signal('');
  readonly travelDate = signal('');

  async ngOnInit(): Promise<void> {
    const p = this.route.snapshot.queryParams;
    this.travelDate.set(p['travelDate'] ?? '');
    try {
      const [cities, results] = await Promise.all([
        this.cityService.getCities(),
        this.scheduleService.searchSchedules({
          fromCityId: p['fromCityId'],
          toCityId: p['toCityId'],
          travelDate: p['travelDate'],
        }),
      ]);
      this.fromCityName.set(cities.find(c => c.cityId === p['fromCityId'])?.cityName ?? p['fromCityId']);
      this.toCityName.set(cities.find(c => c.cityId === p['toCityId'])?.cityName ?? p['toCityId']);
      this.schedules.set(results);
    } catch (err: unknown) {
      this.error.set((err as Error).message);
    } finally {
      this.loading.set(false);
    }
  }

  book(scheduleId: string): void {
    this.router.navigate(['/book', scheduleId]);
  }

  formatTime(iso: string): string {
    return new Date(iso).toLocaleTimeString('en-IN', { hour: '2-digit', minute: '2-digit', hour12: true });
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-IN', { weekday: 'short', day: 'numeric', month: 'short' });
  }
}
