import {
  ChangeDetectionStrategy,
  Component,
} from '@angular/core';

import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-settings-dashboard',
  standalone: true,
  imports: [
    RouterLink,
  ],
  templateUrl: './settings-dashboard.html',
  styleUrl: './settings-dashboard.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SettingsDashboard {}