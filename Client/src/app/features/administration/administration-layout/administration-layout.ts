import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-administration-layout',
  standalone: true,
  imports: [
    RouterLink,
    RouterLinkActive,
    RouterOutlet,
  ],
  templateUrl: './administration-layout.html',
  styleUrl: './administration-layout.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdministrationLayout {}