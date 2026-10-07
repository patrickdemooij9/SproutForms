<script setup lang="ts">
import { isFieldVisible, type FormClientRow } from '@sproutforms/client';
import { useSproutForm } from '../../context';

defineProps<{
    rows: FormClientRow[];
}>();

const form = useSproutForm();
const Field = form.resolveComponent('Field');
</script>

<template>
    <div v-for="(row, rowIndex) in rows" :key="rowIndex" class="form-row">
        <!-- A column hides with its field, so it doesn't take up space -->
        <div
            v-for="column in row.columns"
            :key="column.field.alias"
            :class="['form-col', `col-${column.width}`]"
            data-sf-col
            :hidden="!isFieldVisible(column.field, form.state.value.values, form.state.value.variables) || undefined"
        >
            <component :is="Field" :field="column.field" />
        </div>
    </div>
</template>
