import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CityService } from '../../core/services/city.service';
import { AuthService } from '../../core/services/auth.service';
import { AssistantUiService } from '../../core/services/assistant-ui.service';
import type { City } from '../../shared/models/city.model';

@Component({
  selector: 'app-home',
  imports: [FormsModule],       // •	Use FormsModule because the page contains a form. Without FormsModule, the ngModel directive will not work and will throw an error.
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class HomeComponent implements OnInit {
  private readonly router = inject(Router);               // DI, Used to change pages. HOME -> SEARCH
  private readonly cityService = inject(CityService);    // DI, Used to get the list of cities from the backend.
  private readonly assistantUi = inject(AssistantUiService);
  readonly auth = inject(AuthService);

  readonly cities = signal<City[]>([]);       // stores data from fronted
  fromCityId = '';
  toCityId = '';
  travelDate = '';
  error = '';

  get today(): string {
    return new Date().toISOString().split('T')[0];
  }

  async ngOnInit(): Promise<void> {      // ngOnInit() is a lifecycle hook that is called after the component is initialized. It is used to perform any additional initialization tasks, such as fetching data from a backend service. In this case, it fetches the list of cities from the CityService and stores it in the cities signal.
    try {
      this.cities.set(await this.cityService.getCities());        // ngOnInit() runs automatically when the page opens. To get cities from the backend, we call the getCities() method of the CityService. The result is stored in the cities signal.
    } catch { /* cities load is best-effort */ }
  }

  async search(): Promise<void> {
    if (!this.fromCityId || !this.toCityId || !this.travelDate) {
      this.error = 'Please fill in all fields.';
      return;
    }
    if (this.fromCityId === this.toCityId) {
      this.error = 'Origin and destination must be different.';
      return;
    }
    this.error = '';
    await this.router.navigate(['/search'], {
      queryParams: {
        fromCityId: this.fromCityId,
        toCityId: this.toCityId,
        travelDate: this.travelDate,
      },
    });
  }

  askAi(): void {
    const from = this.cities().find(c => c.cityId === this.fromCityId)?.cityName;
    const to = this.cities().find(c => c.cityId === this.toCityId)?.cityName;
    const prefill = from && to
      ? `Are there any buses from ${from} to ${to}${this.travelDate ? ' on ' + this.travelDate : ''}?`
      : '';
    this.assistantUi.openWithPrefill(prefill);
  }
}
