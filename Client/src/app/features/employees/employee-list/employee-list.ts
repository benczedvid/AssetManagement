import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { EmployeeApiService } from '../data-access/employee-api';
import { GetEmployeesResponse } from '../data-access/models/get-employees-response';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-employee-list',
  imports: [
    FormsModule,
    DatePipe
  ],
  templateUrl: './employee-list.html',
  styleUrl: './employee-list.css'
})
export class EmployeeList implements OnInit {
  private readonly employeeApiService = inject(EmployeeApiService);

  readonly employees = signal<GetEmployeesResponse[]>([]);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly searchTerm = signal('');

  readonly filteredEmployees = computed(() => {
    const term = this.searchTerm()
      .trim()
      .toLocaleLowerCase('hu-HU');

    if (!term) {
      return this.employees();
    }

    return this.employees().filter(employee =>
      employee.fullName
        .toLocaleLowerCase('hu-HU')
        .includes(term) ||
      employee.employeeNumber
        .toLocaleLowerCase('hu-HU')
        .includes(term) ||
      employee.position
        .toLocaleLowerCase('hu-HU')
        .includes(term) ||
      employee.organizationType
        .toLocaleLowerCase('hu-HU')
        .includes(term)
    );
  });

  ngOnInit(): void {
    this.loadEmployees();
  }

  loadEmployees(): void {
    if (this.isLoading()) {
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.employeeApiService.getAll().subscribe({
      next: employees => {
        this.employees.set(employees);
        this.isLoading.set(false);
      },
      error: error => {
        console.error(
          'A dolgozók betöltése sikertelen.',
          error
        );

        this.errorMessage.set(
          'A dolgozók listáját nem sikerült betölteni.'
        );

        this.isLoading.set(false);
      }
    });
  }

  updateSearchTerm(value: string): void {
    this.searchTerm.set(value);
  }

  clearSearch(): void {
    this.searchTerm.set('');
  }
}
