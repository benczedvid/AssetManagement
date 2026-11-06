import { Component, Input, signal, EventEmitter, Output } from '@angular/core';


export interface SideMenuItem {
  label: string;
  key: string | null;
}

@Component({
  selector: 'app-side-menu',
  standalone: true,
  imports: [],
  templateUrl: './side-menu.html',
  styleUrl: './side-menu.css',
})
export class SideMenuComponent {
  @Input({ required: true }) title!: string;
  @Input({ required: true }) items!: SideMenuItem[];
  @Input() selectedKeys: (string | null)[] = [];

  @Output() itemSelected = new EventEmitter<{key: string | null; checked: boolean}>();

  readonly submenuOpen = signal(true);

  toggleSubmenu(): void {
    this.submenuOpen.update(open => !open);
  }
  onItemChange(key: string | null, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.itemSelected.emit({ key, checked });
}
}